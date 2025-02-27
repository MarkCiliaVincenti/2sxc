// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ToSic.Eav.Helpers;
using ToSic.Sxc.Code.Internal.HotBuild;

namespace ToSic.Sxc.Razor.DbgWip;

internal class RuntimeViewCompiler : IViewCompiler
{
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, CompiledViewDescriptor> _precompiledViews;
    private readonly ConcurrentDictionary<string, string> _normalizedPathCache;
    private readonly IFileProvider _fileProvider;
    private readonly RazorProjectEngine _projectEngine;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly AssemblyResolver _assemblyResolver;
    private readonly CSharpCompiler _csharpCompiler;

    public RuntimeViewCompiler(
        IFileProvider fileProvider,
        RazorProjectEngine projectEngine,
        CSharpCompiler csharpCompiler,
        IList<CompiledViewDescriptor> precompiledViews,
        ILogger logger,
        AssemblyResolver assemblyResolver)
    {
        if (precompiledViews == null)
        {
            throw new ArgumentNullException(nameof(precompiledViews));
        }

        _fileProvider = fileProvider ?? throw new ArgumentNullException(nameof(fileProvider));
        _projectEngine = projectEngine ?? throw new ArgumentNullException(nameof(projectEngine));
        _csharpCompiler = csharpCompiler ?? throw new ArgumentNullException(nameof(csharpCompiler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _assemblyResolver = assemblyResolver;


        _normalizedPathCache = new(StringComparer.Ordinal);

        // This is our L0 cache, and is a durable store. Views migrate into the cache as they are requested
        // from either the set of known precompiled views, or by being compiled.
        _cache = new MemoryCache(new MemoryCacheOptions());

        // We need to validate that the all of the precompiled views are unique by path (case-insensitive).
        // We do this because there's no good way to canonicalize paths on windows, and it will create
        // problems when deploying to linux. Rather than deal with these issues, we just don't support
        // views that differ only by case.
        _precompiledViews = new(
            precompiledViews.Count,
            StringComparer.OrdinalIgnoreCase);

        foreach (var precompiledView in precompiledViews)
        {
            //logger.ViewCompilerLocatedCompiledView(precompiledView.RelativePath);

            if (!_precompiledViews.ContainsKey(precompiledView.RelativePath))
            {
                // View ordering has precedence semantics, a view with a higher precedence was
                // already added to the list.
                _precompiledViews.Add(precompiledView.RelativePath, precompiledView);
            }
        }

        if (_precompiledViews.Count == 0)
        {
            //logger.ViewCompilerNoCompiledViewsFound();
        }
    }

    public Task<CompiledViewDescriptor> CompileAsync(string relativePath)
    {
        if (relativePath == null)
        {
            throw new ArgumentNullException(nameof(relativePath));
        }

        // Attempt to lookup the cache entry using the passed in path. This will succeed if the path is already
        // normalized and a cache entry exists.
        if (_cache.TryGetValue(relativePath, out Task<CompiledViewDescriptor> cachedResult))
        {
            return cachedResult;
        }

        var normalizedPath = GetNormalizedPath(relativePath);
        if (_cache.TryGetValue(normalizedPath, out cachedResult))
        {
            return cachedResult;
        }

        // Entry does not exist. Attempt to create one.
        cachedResult = OnCacheMiss(normalizedPath);
        return cachedResult;
    }

    private Task<CompiledViewDescriptor> OnCacheMiss(string normalizedPath)
    {
        ViewCompilerWorkItem item;
        TaskCompletionSource<CompiledViewDescriptor> taskSource;
        MemoryCacheEntryOptions cacheEntryOptions;

        // Safe races cannot be allowed when compiling Razor pages. To ensure only one compilation request succeeds
        // per file, we'll lock the creation of a cache entry. Creating the cache entry should be very quick. The
        // actual work for compiling files happens outside the critical section.
        lock (_cacheLock)
        {
            // Double-checked locking to handle a possible race.
            if (_cache.TryGetValue(normalizedPath, out Task<CompiledViewDescriptor> result))
            {
                return result;
            }

            if (_precompiledViews.TryGetValue(normalizedPath, out var precompiledView))
            {
                //_logger.ViewCompilerLocatedCompiledViewForPath(normalizedPath);
                item = CreatePrecompiledWorkItem(normalizedPath, precompiledView);
            }
            else
            {
                item = CreateRuntimeCompilationWorkItem(normalizedPath);
            }

            // At this point, we've decided what to do - but we should create the cache entry and
            // release the lock first.
            cacheEntryOptions = new();

            Debug.Assert(item.ExpirationTokens != null);
            for (var i = 0; i < item.ExpirationTokens.Count; i++)
            {
                cacheEntryOptions.ExpirationTokens.Add(item.ExpirationTokens[i]);
            }

            taskSource = new(creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);
            if (item.SupportsCompilation)
            {
                // We'll compile in just a sec, be patient.
            }
            else
            {
                // If we can't compile, we should have already created the descriptor
                Debug.Assert(item.Descriptor != null);
                taskSource.SetResult(item.Descriptor);
            }

            _cache.Set(normalizedPath, taskSource.Task, cacheEntryOptions);
        }

        // Now the lock has been released so we can do more expensive processing.
        if (item.SupportsCompilation)
        {
            Debug.Assert(taskSource != null);

            if (item.Descriptor?.Item != null && ChecksumValidator.IsItemValid(_projectEngine.FileSystem, item.Descriptor.Item))
            {
                // If the item has checksums to validate, we should also have a precompiled view.
                Debug.Assert(item.Descriptor != null);

                taskSource.SetResult(item.Descriptor);
                return taskSource.Task;
            }

            //_logger.ViewCompilerInvalidingCompiledFile(item.NormalizedPath);
            try
            {
                var descriptor = CompileAndEmit(normalizedPath);
                descriptor.ExpirationTokens = cacheEntryOptions.ExpirationTokens;
                taskSource.SetResult(descriptor);
            }
            catch (Exception ex)
            {
                taskSource.SetException(ex);
            }
        }

        return taskSource.Task;
    }

    private ViewCompilerWorkItem CreatePrecompiledWorkItem(string normalizedPath, CompiledViewDescriptor precompiledView)
    {
        // We have a precompiled view - but we're not sure that we can use it yet.
        //
        // We need to determine first if we have enough information to 'recompile' this view. If that's the case
        // we'll create change tokens for all of the files.
        //
        // Then we'll attempt to validate if any of those files have different content than the original sources
        // based on checksums.
        if (precompiledView.Item == null || !ChecksumValidator.IsRecompilationSupported(precompiledView.Item))
        {
            return new()
            {
                // If we don't have a checksum for the primary source file we can't recompile.
                SupportsCompilation = false,

                ExpirationTokens = Array.Empty<IChangeToken>(), // Never expire because we can't recompile.
                Descriptor = precompiledView, // This will be used as-is.
            };
        }

        var item = new ViewCompilerWorkItem()
        {
            SupportsCompilation = true,

            Descriptor = precompiledView, // This might be used, if the checksums match.

            // Used to validate and recompile
            NormalizedPath = normalizedPath,

            ExpirationTokens = GetExpirationTokens(precompiledView),
        };

        // We also need to create a new descriptor, because the original one doesn't have expiration tokens on
        // it. These will be used by the view location cache, which is like an L1 cache for views (this class is
        // the L2 cache).
        item.Descriptor = new()
        {
            ExpirationTokens = item.ExpirationTokens,
            Item = precompiledView.Item,
            RelativePath = precompiledView.RelativePath,
        };

        return item;
    }

    private ViewCompilerWorkItem CreateRuntimeCompilationWorkItem(string normalizedPath)
    {
        IList<IChangeToken> expirationTokens = new List<IChangeToken>
        {
            _fileProvider.Watch(normalizedPath),
        };

        var projectItem = _projectEngine.FileSystem.GetItem(normalizedPath, fileKind: null);
        if (!projectItem.Exists)
        {
            //_logger.ViewCompilerCouldNotFindFileAtPath(normalizedPath);

            // If the file doesn't exist, we can't do compilation right now - we still want to cache
            // the fact that we tried. This will allow us to re-trigger compilation if the view file
            // is added.
            return new()
            {
                // We don't have enough information to compile
                SupportsCompilation = false,

                Descriptor = new()
                {
                    RelativePath = normalizedPath,
                    ExpirationTokens = expirationTokens,
                },

                // We can try again if the file gets created.
                ExpirationTokens = expirationTokens,
            };
        }

        //_logger.ViewCompilerFoundFileToCompile(normalizedPath);

        GetChangeTokensFromImports(expirationTokens, projectItem);

        return new()
        {
            SupportsCompilation = true,

            NormalizedPath = normalizedPath,
            ExpirationTokens = expirationTokens,
        };
    }

    private IList<IChangeToken> GetExpirationTokens(CompiledViewDescriptor precompiledView)
    {
        var checksums = precompiledView.Item.GetChecksumMetadata();
        var expirationTokens = new List<IChangeToken>(checksums.Count);

        for (var i = 0; i < checksums.Count; i++)
        {
            // We rely on Razor to provide the right set of checksums. Trust the compiler, it has to do a good job,
            // so it probably will.
            expirationTokens.Add(_fileProvider.Watch(checksums[i].Identifier));
        }

        return expirationTokens;
    }

    private void GetChangeTokensFromImports(IList<IChangeToken> expirationTokens, RazorProjectItem projectItem)
    {
        // OK this means we can do compilation. For now let's just identify the other files we need to watch
        // so we can create the cache entry. Compilation will happen after we release the lock.
        var importFeature = _projectEngine.ProjectFeatures.OfType<IImportProjectFeature>().ToArray();
        foreach (var feature in importFeature)
        {
            foreach (var file in feature.GetImports(projectItem))
            {
                if (file.FilePath != null)
                {
                    expirationTokens.Add(_fileProvider.Watch(file.FilePath));
                }
            }
        }
    }

    protected virtual CompiledViewDescriptor CompileAndEmit(string relativePath)
    {
        var projectItem = _projectEngine.FileSystem.GetItem(relativePath, fileKind: null);
        var codeDocument = _projectEngine.Process(projectItem);
        var cSharpDocument = codeDocument.GetCSharpDocument();

        if (cSharpDocument.Diagnostics.Count > 0)
        {
            throw CompilationFailedExceptionFactory.Create(
                codeDocument,
                cSharpDocument.Diagnostics);
        }

        var assembly = CompileAndEmit(codeDocument, cSharpDocument.GeneratedCode, GetMetadataReferences(relativePath));

        // Anything we compile from source will use Razor 2.1 and so should have the new metadata.
        var loader = new RazorCompiledItemLoader();
        var item = loader.LoadItems(assembly).Single();
        return new(item);
    }

    /// <summary>
    /// get MetadataReferences for relativePath
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns></returns>
    private IEnumerable<MetadataReference> GetMetadataReferences(string relativePath)
    {
        IEnumerable<MetadataReference> references = new List<MetadataReference>();
        var appCodePath = _assemblyResolver.GetAssemblyLocation(GetAppRelativePath(relativePath));
        if (appCodePath != null)
            references = references.Append(MetadataReference.CreateFromFile(appCodePath));
        return references;
    }

    /// <summary>
    /// extract appRelativePath from relativePath
    /// </summary>
    /// <param name="relativePath">string "/2sxc/n/aaa-folder-name/etc..."</param>
    /// <returns>string "2sxc\\n\\aaa-folder-name" or null</returns>
    private static string GetAppRelativePath(string relativePath)
    {
        // TODO: stv, this has to more generic because it is very 2sxc on Oqtane specific
        // validations
        if ((string.IsNullOrEmpty(relativePath))
            || (relativePath.Length < 8)
            || (relativePath[0] != '/')
            || (relativePath[5] != '/'))
            throw new($"relativePath:'{relativePath}' is not in format '/2sxc/n/app-folder-name/etc...'");

        // find position of 4th slash in relativePath 
        var pos = 6; // skipping first 2 slashes
        for (var i = 0; i < 2; i++)
        {
            pos = relativePath.IndexOf('/', pos + 1);
            if (pos < 0) 
                throw new($"relativePath:'{relativePath}' is not in format '/2sxc/n/app-folder-name/etc...'");
        }

        return relativePath.Substring(1, pos - 1).Backslash();
    }

    internal Assembly CompileAndEmit(RazorCodeDocument codeDocument, string generatedCode, IEnumerable<MetadataReference> references)
    {
        //_logger.GeneratedCodeToAssemblyCompilationStart(codeDocument.Source.FilePath);

        var startTimestamp = _logger.IsEnabled(LogLevel.Debug) ? Stopwatch.GetTimestamp() : 0;

        var assemblyName = Path.GetRandomFileName();
        var compilation = CreateCompilation(generatedCode, assemblyName, references);

        var emitOptions = _csharpCompiler.EmitOptions;
        var emitPdbFile = _csharpCompiler.EmitPdb && emitOptions.DebugInformationFormat != DebugInformationFormat.Embedded;

        using var assemblyStream = new MemoryStream();
        using var pdbStream = emitPdbFile ? new MemoryStream() : null;
        var result = compilation.Emit(
            assemblyStream,
            pdbStream,
            options: emitOptions);

        if (!result.Success)
        {
            throw CompilationFailedExceptionFactory.Create(
                codeDocument,
                generatedCode,
                assemblyName,
                result.Diagnostics);
        }

        assemblyStream.Seek(0, SeekOrigin.Begin);
        pdbStream?.Seek(0, SeekOrigin.Begin);

        var assembly = Assembly.Load(assemblyStream.ToArray(), pdbStream?.ToArray());
        //_logger.GeneratedCodeToAssemblyCompilationEnd(codeDocument.Source.FilePath, startTimestamp);

        return assembly;
    }

    private CSharpCompilation CreateCompilation(string compilationContent, string assemblyName, IEnumerable<MetadataReference> references)
    {
        var sourceText = SourceText.From(compilationContent, Encoding.UTF8);
        var syntaxTree = _csharpCompiler.CreateSyntaxTree(sourceText).WithFilePath(assemblyName);
        return _csharpCompiler
            .CreateCompilation(assemblyName)
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(references);
    }

    private string GetNormalizedPath(string relativePath)
    {
        Debug.Assert(relativePath != null);
        if (relativePath.Length == 0)
        {
            return relativePath;
        }

        if (!_normalizedPathCache.TryGetValue(relativePath, out var normalizedPath))
        {
            normalizedPath = ViewPath.NormalizePath(relativePath);
            _normalizedPathCache[relativePath] = normalizedPath;
        }

        return normalizedPath;
    }

    private class ViewCompilerWorkItem
    {
        public bool SupportsCompilation { get; set; } = default!;

        public string NormalizedPath { get; set; } = default!;

        public IList<IChangeToken> ExpirationTokens { get; set; } = default!;

        public CompiledViewDescriptor Descriptor { get; set; } = default!;
    }
}
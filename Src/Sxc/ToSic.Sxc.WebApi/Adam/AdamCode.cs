﻿using System;
using System.IO;
using ToSic.Eav;
using ToSic.Eav.Configuration;
using ToSic.Eav.Documentation;
using ToSic.Eav.Logging;
using ToSic.Eav.Plumbing;
using ToSic.Sxc.Adam;
using ToSic.Sxc.Code;

namespace ToSic.Sxc.WebApi.Adam
{
    /// <summary>
    /// Adam Shared Code Across the APIs
    /// See docs of official interface <see cref="IDynamicWebApi"/>
    /// </summary>
    [PrivateApi("Used by DynamicApiController and Hybrid.Api12_DynCode")]
    public class AdamCode: HasLog
    {
        // ReSharper disable once InconsistentNaming
        public IDynamicCodeRoot _DynCodeRoot { get; private set; }
        public IServiceProvider ServiceProvider { get; }

        public AdamCode(IServiceProvider serviceProvider) : base("AdamCode")
        {
            ServiceProvider = serviceProvider;
        }

        public AdamCode Init(IDynamicCodeRoot dynCodeRoot, ILog parentLog)
        {
            Log.LinkTo(parentLog);
            _DynCodeRoot = dynCodeRoot;

            return this;
        }

        public IFile SaveInAdam(string dontRelyOnParameterOrder = Parameters.Protector,
            Stream stream = null,
            string fileName = null,
            string contentType = null,
            Guid? guid = null,
            string field = null,
            string subFolder = "")
        {
            Parameters.ProtectAgainstMissingParameterNames(dontRelyOnParameterOrder, "SaveInAdam",
                $"{nameof(stream)},{nameof(fileName)},{nameof(contentType)},{nameof(guid)},{nameof(field)},{nameof(subFolder)} (optional)");

            if (stream == null || fileName == null || contentType == null || guid == null || field == null)
                throw new Exception();

            var feats = new[] { FeatureIds.UseAdamInWebApi, FeatureIds.PublicUpload };
            var features = _DynCodeRoot.GetService<ToSic.Eav.Configuration.Features>();

            if (!/*Features*/features.EnabledOrException(feats, "can't save in ADAM", out var exp))
                throw exp;

            var appId = _DynCodeRoot?.Block?.AppId ?? _DynCodeRoot?.App?.AppId ?? throw new Exception("Error, SaveInAdam needs an App-Context to work, but the App is not known.");
            return ServiceProvider.Build<AdamTransUpload<int, int>>()
                .Init(appId, contentType, guid.Value, field, false, Log)
                .UploadOne(stream, fileName, subFolder, true);
        }
    }
}
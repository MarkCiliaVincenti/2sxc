﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using ToSic.Eav.Context;
using ToSic.Eav.Logging;
using ToSic.Eav.Persistence.Logging;
using ToSic.Eav.Plumbing;
using ToSic.Eav.WebApi.Assets;
using ToSic.Eav.WebApi.Context;
using ToSic.Eav.WebApi.Dto;
using ToSic.Sxc.Apps.Blocks;
using ToSic.Sxc.Blocks;
using ToSic.Sxc.WebApi.Adam;
using ToSic.Sxc.WebApi.Usage;
using ToSic.Sxc.WebApi.Views;

namespace ToSic.Sxc.WebApi.Admin
{
    public class ViewControllerReal : HasLog<ViewControllerReal>, IViewController
    {
        public const string LogSuffix = "View";

        public ViewControllerReal(
            LazyInitLog<IContextOfSite> context,
            LazyInitLog<ViewsBackend> viewsBackend, 
            LazyInitLog<ViewsExportImport> viewExportImport, 
            LazyInitLog<UsageBackend> usageBackend, 
            LazyInitLog<PolymorphismBackend> polymorphismBackend
            ) : base("Api.ViewRl")
        {
            _context = context.SetLog(Log);
            _polymorphismBackend = polymorphismBackend.SetLog(Log);
            _usageBackend = usageBackend.SetLog(Log);
            _viewsBackend = viewsBackend.SetLog(Log);
            _viewExportImport = viewExportImport.SetLog(Log);
        }
        private readonly LazyInitLog<IContextOfSite> _context;
        private readonly LazyInitLog<PolymorphismBackend> _polymorphismBackend;
        private readonly LazyInitLog<UsageBackend> _usageBackend;
        private readonly LazyInitLog<ViewsBackend> _viewsBackend;
        private readonly LazyInitLog<ViewsExportImport> _viewExportImport;

        /// <inheritdoc />
        public IEnumerable<ViewDetailsDto> All(int appId) => _viewsBackend.Ready.GetAll(appId);

        /// <inheritdoc />
        public PolymorphismDto Polymorphism(int appId) => _polymorphismBackend.Ready.Polymorphism(appId);

        /// <inheritdoc />
        public bool Delete(int appId, int id) => _viewsBackend.Ready.Delete(appId, id);

        /// <inheritdoc />
        public HttpResponseMessage Json(int appId, int viewId) => _viewExportImport.Ready.DownloadViewAsJson(appId, viewId);

        /// <summary>
        /// This method is not implemented for ControllerReal, because ControllerReal implements Import(HttpUploadedFile uploadInfo, int zoneId, int appId)
        /// </summary>
        /// <param name="zoneId"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public ImportResultDto Import(int zoneId, int appId) => throw new NotImplementedException();

        /// <summary>
        /// This implementation is special ControllerReal, instead of ImportResultDto Import(int zoneId, int appId) that is not implemented.
        /// </summary>
        /// <param name="uploadInfo"></param>
        /// <param name="zoneId"></param>
        /// <param name="appId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public ImportResultDto Import(HttpUploadedFile uploadInfo, int zoneId, int appId)
        {
            var wrapLog = Log.Call<ImportResultDto>();

            if (PreventServerTimeout300 == null)
            {
                Log.Add("Error, PreventServerTimeout300 implementation is not set.");
                throw new ArgumentException("PreventServerTimeout300 implementation is not set.");
            }
            PreventServerTimeout300();

            if (!uploadInfo.HasFiles())
                return new ImportResultDto(false, "no file uploaded", Message.MessageTypes.Error);

            var streams = new List<FileUploadDto>();
            for (var i = 0; i < uploadInfo.Count; i++)
            {
                var (fileName, stream) = uploadInfo.GetStream(i);
                streams.Add(new FileUploadDto {Name = fileName, Stream = stream});
            }
            var result = _viewExportImport.Ready.ImportView(zoneId, appId, streams, _context.Ready.Site.DefaultCultureCode);

            return wrapLog("ok", result);
        }

        /// <summary>
        /// Special init to give it a implementation how to extend the server timeout - used in Dnn, probably not used in Oqtane.
        /// Only needed for the Import.
        /// Todo: Review if this should really be here, or move back to Dnn as it's probably only used there
        /// </summary>
        /// <param name="preventServerTimeout300"></param>
        /// <returns></returns>
        public ViewControllerReal ImportPrep(Action preventServerTimeout300)
        {
            PreventServerTimeout300 = preventServerTimeout300;
            return this;
        }
        private Action PreventServerTimeout300 { get; set; }


        /// <inheritdoc />
        public IEnumerable<ViewDto> Usage(int appId, Guid guid)
        {
            if (FinalBuilder == null)
            {
                Log.Add("Error, FinalBuilder implementation is not set.");
                throw new ArgumentException("FinalBuilder implementation is not set.");
            }
            return _usageBackend.Ready.ViewUsage(appId, guid, FinalBuilder);
        }
        public ViewControllerReal UsagePreparations(Func<List<IView>, List<BlockConfiguration>, IEnumerable<ViewDto>> finalBuilder)
        {
            FinalBuilder = finalBuilder;
            return this;
        }
        private Func<List<IView>, List<BlockConfiguration>, IEnumerable<ViewDto>> FinalBuilder { get; set; }

        /// <summary>
        /// Helper method to get SiteId for ControllerReal proxy class.
        /// </summary>
        public int SiteId => _context.Ready.Site.Id;
    }
}
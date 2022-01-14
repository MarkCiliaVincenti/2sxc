﻿using System;
using ToSic.Sxc.Blocks.Output;

namespace ToSic.Sxc.Oqt.Server.Blocks.Output
{
    public class OqtBlockResourceExtractor: BlockResourceExtractor
    {
        public override Tuple<string, bool> Process(string renderedTemplate)
        {
            var include2SxcJs = false;

            ExtractOnlyEnableOptimization = false;

            // Handle Client Dependency injection
            renderedTemplate = ExtractExternalScripts(renderedTemplate, ref include2SxcJs);

            // Handle inline JS
            renderedTemplate = ExtractInlineScripts(renderedTemplate);

            // Handle Styles
            renderedTemplate = ExtractStyles(renderedTemplate);

            Assets.ForEach(a => a.PosInPage = OqtPositionName(a.PosInPage));

            return new Tuple<string, bool>(renderedTemplate, include2SxcJs);
        }



        private string OqtPositionName(string position)
        {
            position = position.ToLowerInvariant();

            return position switch
            {
                "body" => position,
                "head" => position,
                _ => "body"
            };
        }

    }

}
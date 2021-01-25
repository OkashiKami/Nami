﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Imgur.API.Authentication.Impl;
using Imgur.API.Endpoints.Impl;
using Imgur.API.Enums;
using Imgur.API.Models;
using Nami.Services;

namespace Nami.Modules.Search.Services
{
    public sealed class ImgurService : INamiService
    {
        public bool IsDisabled => this.imgur is null;

        private readonly ImgurClient? imgur;
        //private readonly ImageEndpoint? iEndpoint;
        private readonly GalleryEndpoint? gEndpoint;


        public ImgurService(BotConfigService cfg)
        {
            if (!string.IsNullOrWhiteSpace(cfg.CurrentConfiguration.ImgurKey)) {
                this.imgur = new ImgurClient(cfg.CurrentConfiguration.ImgurKey);
                //this.iEndpoint = new ImageEndpoint(this.imgur);
                this.gEndpoint = new GalleryEndpoint(this.imgur);
            }
        }


        public async Task<IEnumerable<IGalleryItem>?> GetItemsFromSubAsync(string sub, int amount, SubredditGallerySortOrder order, TimeWindow tw)
        {
            if (this.IsDisabled || this.gEndpoint is null)
                return null;
            
            if (amount is < 1 or > 10)
                amount = 10;

            IEnumerable<IGalleryItem> images = await this.gEndpoint.GetSubredditGalleryAsync(sub, order, tw)
                .ConfigureAwait(false);
            return images.Take(amount);
        }
    }
}

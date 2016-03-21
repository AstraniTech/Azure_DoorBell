using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.WindowsAzure.Mobile.Service;
using iotpocService.DataObjects;
using iotpocService.Models;
using System;
using Microsoft.WindowsAzure;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace iotpocService.Controllers
{
    public class PicturesController : TableController<Pictures>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            iotpocContext context = new iotpocContext();
            DomainManager = new EntityDomainManager<Pictures>(context, Request, Services);
        }

        // GET tables/Pictures
        public IQueryable<Pictures> GetAllPictures()
        {
            return Query(); 
        }

        // GET tables/Pictures/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public SingleResult<Pictures> GetPictures(string id)
        {
            return Lookup(id);
        }

        // PATCH tables/Pictures/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task<Pictures> PatchPictures(string id, Delta<Pictures> patch)
        {
             return UpdateAsync(id, patch);
        }

        // POST tables/Pictures
        public async Task<IHttpActionResult> PostPictures(Pictures item)
        {
            Pictures current = await InsertAsync(item);

           
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }

        // DELETE tables/Pictures/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task DeletePictures(string id)
        {
             return DeleteAsync(id);
        }

    }
}
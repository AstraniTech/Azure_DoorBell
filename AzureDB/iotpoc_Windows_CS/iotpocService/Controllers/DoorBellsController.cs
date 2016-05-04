using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.WindowsAzure.Mobile.Service;
using iotpocService.DataObjects;
using iotpocService.Models;

namespace iotpocService.Controllers
{
    public class DoorBellsController : TableController<DoorBells>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            iotpocContext context = new iotpocContext();
            DomainManager = new EntityDomainManager<DoorBells>(context, Request, Services);
        }

        // GET tables/DoorBells
        public IQueryable<DoorBells> GetAllDoorBells()
        {
            return Query(); 
        }

        // GET tables/DoorBells/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public SingleResult<DoorBells> GetDoorBells(string id)
        {
            return Lookup(id);
        }

        // PATCH tables/DoorBells/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task<DoorBells> PatchDoorBells(string id, Delta<DoorBells> patch)
        {
             return UpdateAsync(id, patch);
        }

        // POST tables/DoorBells
        public async Task<IHttpActionResult> PostDoorBells(DoorBells item)
        {
            DoorBells current = await InsertAsync(item);
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }

        // DELETE tables/DoorBells/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task DeleteDoorBells(string id)
        {
             return DeleteAsync(id);
        }

    }
}
using Newtonsoft.Json;

using System.ComponentModel.DataAnnotations;

namespace Mango.Services.OrderAPI.Models.Dto
{
    public class ProductDto
    {
        //This is needed since Entity model has some other column name
        //Response conatins exact table record matching column names that are in entity model
        //and hence in table. Revealing potential details.
        [JsonProperty("id")]
        public int ProductId { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
        //TODO: Image uploading part is missing. Do yourself
        public string ImageUrl { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Since its a DTO, we can add and fields and AutoMapper
        /// dynamically handles by matching names and additional attributes provided
        /// </summary>
        public int Count { get; set; } = 1;
    }
}

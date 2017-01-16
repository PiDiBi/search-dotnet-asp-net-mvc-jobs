using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Web;

namespace NYCJobsWeb
{
    public class ProductsSearch
    {
        private static SearchServiceClient _searchClient;
        private static ISearchIndexClient _indexClient;
        private static string IndexName = "temp";
        
        public static string errorMessage;

        static ProductsSearch()
        {
            try
            {
                string searchServiceName = ConfigurationManager.AppSettings["OriSearchServiceName"];
                string apiKey = ConfigurationManager.AppSettings["OriSearchServiceApiKey"];

                // Create an HTTP reference to the catalog index
                _searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(apiKey));
                _indexClient = _searchClient.Indexes.GetClient(IndexName);

            }
            catch (Exception e)
            {
                errorMessage = e.Message.ToString();
            }
        }

        public DocumentSearchResult Search(string searchText, 
            string productParentCategoriesFacet, 
            string priceRangeFacet, 
            string skuColorFacet,
            string sortType, int currentPage)
        {
            // Execute search based on query string
            //https://docs.microsoft.com/en-us/rest/api/searchservice/odata-expression-syntax-for-azure-search
            try
            {
                SearchParameters sp = new SearchParameters()
                {
                    SearchMode = SearchMode.Any,
                    Top = 10,
                    Skip = currentPage - 1,
                    // Limit results
                    Select = new List<String>() {"id", "ProductId", "SkuDisplayName", "ProductDescription", "ProductParentCategories",
                        "ProductRating", "ProductIngredients", "SkuName", "SkuDisplayName", "SkuColor",
                        "SkuPrice", "SkuOrigPrice", "SkuPriceFormated", "ProductSize1Image", "ProductSize4Image"},
                    // Add count
                    IncludeTotalResultCount = true,
                    // Add search highlights
                    HighlightFields = new List<String>() { "ProductDescription" },
                    HighlightPreTag = "<b>",
                    HighlightPostTag = "</b>",
                    // Add facets
                    Facets = new List<String>() { "ProductParentCategories", "SkuColor", "SkuPrice,interval:100" },
                };
                // Define the sort type
                //if (sortType == "featured")
                //{
                //    sp.ScoringProfile = "jobsScoringFeatured";      // Use a scoring profile
                //    sp.ScoringParameters = new List<ScoringParameter>();
                //    sp.ScoringParameters.Add(new ScoringParameter("featuredParam", new[] { "featured" }));
                //    sp.ScoringParameters.Add(new ScoringParameter("mapCenterParam", GeographyPoint.Create(lon, lat)));
                //}
                //else 
                if (sortType == "priceDesc")
                    sp.OrderBy = new List<String>() { "SkuPrice desc" };
                else if (sortType == "priceIncr")
                    sp.OrderBy = new List<String>() { "SkuPrice" };
                else if (sortType == "productRating")
                    sp.OrderBy = new List<String>() { "ProductRating desc" };

                //productParentCategoriesFacet, string productRatingFacet, 
                //string productIngredientsFacet, string priceRangeFacet, string skuColorFacet,

                // Add filtering
                string filter = "SkuDontSell eq false";
                if (productParentCategoriesFacet != "")
                {
                    filter += " and ";
                    filter += " ProductParentCategories/any(c: c eq '" + productParentCategoriesFacet + "')";
                }
                if (priceRangeFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "SkuPrice ge " + priceRangeFacet + " and SkuPrice lt " + (Convert.ToInt32(priceRangeFacet) + 100).ToString();
                }

                if (skuColorFacet != "")
                {
                    if (filter != null)
                        filter += " and ";
                    filter += "SkuColor eq '" + skuColorFacet + "'";

                }

                sp.Filter = filter;

                return _indexClient.Documents.Search(searchText, sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
                throw;
            }
            return null;
        }
        public DocumentSuggestResult Suggest(string searchText, bool fuzzy)
        {
            // Execute search based on query string
            try
            {
                SuggestParameters sp = new SuggestParameters()
                {
                    UseFuzzyMatching = fuzzy,
                    Top = 8,
                    Filter = "SkuDontSell eq false"
                };

                return _indexClient.Documents.Suggest(searchText, "sg", sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

        public Document LookUp(string id)
        {
            // Execute geo search based on query string
            try
            {
                return _indexClient.Documents.Get(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error querying index: {0}\r\n", ex.Message.ToString());
            }
            return null;
        }

    }
}
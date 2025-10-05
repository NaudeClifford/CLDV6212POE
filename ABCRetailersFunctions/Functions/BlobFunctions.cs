using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABCRetailersFunctions.Functions;

public class BlobFunctions
{

    [Function("OnProductImageUpload")]
    public void OnProductImageUploaded(
        [BlobTrigger("%BLOB_PRODUCT_IMAGES%/{name}", Connection = "STORAGE_CONNECTION")] 
        Stream blob, 
        string name,
        FunctionContext con)
    {
        var log = con.GetLogger("OnProductImageUpload");
        log.LogInformation($"Product image uploaded: {name}, size={blob.Length} bytes");
    }
}
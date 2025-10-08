using ABCRetails.Models;
using ABCRetails.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetails.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionApi _api;

        public UploadController(IFunctionApi api) => _api = api;

        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost, ValidateAntiForgeryToken]

        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid) return View(model);
            
            try
            {
                if (model.ProofOfPayment == null || model.ProofOfPayment.Length < 0)
                {
                    ModelState.AddModelError("ProofOfPayment", "Please select a file to upload");
                    return View(model);
                }

                var fileName = await _api.UploadProofOfPaymentAsync(
                    model.ProofOfPayment,
                    model.OrderId,
                    model.CustomerName
                );

                TempData["Success"] = $"File upload successfully! File name: {fileName}";

                return View(new FileUploadModel());

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating customer: {ex.Message}");
                return View(model);
            }
        }
    }
}

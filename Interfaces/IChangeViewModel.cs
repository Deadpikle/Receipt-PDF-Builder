using ReceiptPDFBuilder.ViewModels;

namespace ReceiptPDFBuilder.Interfaces
{
    interface IChangeViewModel
    {
        void PushViewModel(BaseViewModel model);
        void PopViewModel();
    }
}
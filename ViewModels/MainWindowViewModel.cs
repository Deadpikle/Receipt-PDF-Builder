using ReceiptPDFBuilder.Helpers;
using ReceiptPDFBuilder.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReceiptPDFBuilder.ViewModels
{
    class MainWindowViewModel : ChangeNotifier, IChangeViewModel
    {
        BaseViewModel _currentViewModel;
        Stack<BaseViewModel> _viewModels;

        public MainWindowViewModel(ITopLevelGrabber topLevelGrabber)
        {
            _viewModels = new Stack<BaseViewModel>();
            var initialViewModel = new MainViewModel(this)
            {
                TopLevelGrabber = topLevelGrabber
            };
            _viewModels.Push(initialViewModel);
            _currentViewModel = initialViewModel;
        }

        public BaseViewModel CurrentViewModel
        {
            get { return _currentViewModel; }
            set { _currentViewModel = value; NotifyPropertyChanged(); }
        }

        #region IChangeViewModel

        public void PushViewModel(BaseViewModel model)
        {
            _viewModels.Push(model);
            CurrentViewModel = model;
        }

        public void PopViewModel()
        {
            if (_viewModels.Count > 1)
            {
                _viewModels.Pop();
                CurrentViewModel = _viewModels.Peek();
            }
        }

        #endregion
    }
}

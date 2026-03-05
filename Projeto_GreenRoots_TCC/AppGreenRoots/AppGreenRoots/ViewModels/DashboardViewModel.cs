using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using AppGreenRoots.Commands;
using AppGreenRoots.Models;

namespace AppGreenRoots.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly Usuario _usuario;
    public string NomeUsuario  => _usuario.Nome;
    public string EmailUsuario => _usuario.Email;

    public ICommand LogoutCommand { get; }

    public DashboardViewModel(Usuario usuario)
    {
        _usuario      = usuario;
        LogoutCommand = new RelayCommand(_ => ExecutarLogout());
        
    }

    private void ExecutarLogout()
    {
        var login = new Views.LoginView();
        login.Show();
        foreach (Window w in Application.Current.Windows)
            if (w is Views.DashboardView) { w.Close(); break; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    
    
}
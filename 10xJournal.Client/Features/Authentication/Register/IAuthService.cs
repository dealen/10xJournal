namespace _10xJournal.Client.Features.Authentication.Register;

public interface IAuthService
{
    Task RegisterAsync(string email, string password);
}

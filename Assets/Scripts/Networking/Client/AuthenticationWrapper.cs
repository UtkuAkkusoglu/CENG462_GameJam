using UnityEngine;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

public static class AuthenticationWrapper
{
    public enum AuthState
    {
        NotAuthenticated,  // Henüz giriş yapılmadı
        Authenticating,    // Giriş işlemi sürüyor
        Authenticated,     // Başarılı giriş
        Error,             // Hata oluştu
        Timeout            // Giriş süresi doldu
    }

    public static AuthState CurrentAuthState { get; private set; } = AuthState.NotAuthenticated; // "AuthState.NotAuthenticated;" değişkene başlangıç değeri atıyor.

    public static async Task<AuthState> DoAuth(int maxRetries = 5)
    {
        // Eğer zaten authenticate edilmişse, mevcut durumu döndür
        if (CurrentAuthState == AuthState.Authenticated)
            return CurrentAuthState;

        if (CurrentAuthState == AuthState.Authenticating)
            return await WaitForAuthentication();

        await SignInAnonymouslyAsync(maxRetries);
        return CurrentAuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries)
    {
        CurrentAuthState = AuthState.Authenticating;
        int retries = 0;

        while (CurrentAuthState == AuthState.Authenticating && retries < maxRetries)
        {
            // Anonim olarak giriş yapmayı dene
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn &&
                    AuthenticationService.Instance.IsAuthorized)
                {
                    CurrentAuthState = AuthState.Authenticated;
                    return; // Başarılı giriş yapıldı, metottan çık
                }

                retries++;
                await Task.Delay(1000); // tekrar denemeden önce 1 saniye bekle
            }
            catch (AuthenticationException authEx)
            {
                Debug.LogError(authEx);
                CurrentAuthState = AuthState.Error;
                return;  // hem bu hem alttaki return slaytta yoktu, ben ekledim.
            }
            catch (RequestFailedException reqEx)
            {
                Debug.LogError(reqEx);
                CurrentAuthState = AuthState.Error;
                return;
            }
        }

        if(CurrentAuthState != AuthState.Authenticated)
        {
            CurrentAuthState = AuthState.Timeout; // maxRetries a ulaşıldıysa timeout olarak işaretle
            Debug.LogWarning($"Player was not signed in successfully after {maxRetries} retries");
        }
    }

    private static async Task<AuthState> WaitForAuthentication()  // Slaytta adı Authenticating()'di.
    {
        Debug.LogWarning("Already authenticating...");

        while (CurrentAuthState == AuthState.Authenticating || CurrentAuthState == AuthState.NotAuthenticated) // Race condition önleniyor.
        {
            await Task.Delay(200);
        }

        return CurrentAuthState;
    }
}

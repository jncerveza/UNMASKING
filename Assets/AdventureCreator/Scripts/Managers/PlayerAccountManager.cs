using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;

public class PlayerAccountManager : MonoBehaviour
{
 
    private async void Start()
    {
        // Call the async method to handle initialization
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();
        Debug.Log("Unity Services initialized.");

        // Subscribe to PlayerAccountService SignedIn event
        PlayerAccountService.Instance.SignedIn += async () => await OnUnityAccountSignedIn();

        // Check if the player is already signed in
        if (AuthenticationService.Instance.IsSignedIn && !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerId))
        {
            Debug.Log("Player is already signed in with PlayerId: " + AuthenticationService.Instance.PlayerId);
            // Proceed to retrieve player information
            await OnUnityAccountSignedIn();
        }
        else
        {
            // Player is not signed in, prompt for sign-in
            await InitSignIn();
        }
    }

    public async Task InitSignIn()
    {
        try
        {
            // Open Unity Player Account sign-in portal
            await PlayerAccountService.Instance.StartSignInAsync();
            Debug.Log("Player account sign-in initiated.");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error initiating Unity Player Account sign-in: " + ex.Message);
        }
    }

    private async Task OnUnityAccountSignedIn()
    {
        try
        {
            // Retrieve the access token for Unity account sign-in
            string accessToken = PlayerAccountService.Instance.AccessToken;
            Debug.Log("Access token retrieved: " + accessToken);

            // Use the access token to sign in
            await SignInWithUnityAsync(accessToken);
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during Unity account sign-in: " + ex.Message);
        }
    }

    private async Task SignInWithUnityAsync(string accessToken)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(accessToken);
            Debug.Log("Sign-in with Unity successful.");

            // Retrieve player information
            string playerName = await AuthenticationService.Instance.GetPlayerNameAsync();

        }
        catch (AuthenticationException ex)
        {
            Debug.LogError("Authentication exception during Unity sign-in: " + ex.Message);
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError("Request failed during Unity sign-in: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from SignedIn event
        PlayerAccountService.Instance.SignedIn -= async () => await OnUnityAccountSignedIn();
    }
}

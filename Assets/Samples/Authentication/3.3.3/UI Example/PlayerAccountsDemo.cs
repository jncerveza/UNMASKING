using System;
using System.Text;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

namespace Unity.Services.Authentication.PlayerAccounts.Samples
{
    class PlayerAccountsDemo : MonoBehaviour
    {
        [SerializeField]
        Text m_StatusText;
        [SerializeField]
        Text m_ExceptionText;
        [SerializeField]
        GameObject m_SignOut;
        [SerializeField]
        Toggle m_PlayerAccountSignOut;

        string m_ExternalIds;

        async void Awake()
        {
            await UnityServices.InitializeAsync();
        }

        public async void StartSignInAsync()
        {
            // Check if already signed in before starting sign-in process
            if (PlayerAccountService.Instance.IsSignedIn && AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Already signed in.");
                return; // Exit early if already signed in
            }

            try
            {
                await PlayerAccountService.Instance.StartSignInAsync();
                // After successful sign-in, check if the player was previously signed in anonymously
                if (AuthenticationService.Instance.IsSignedIn)
                {
                    // Link with Unity using the access token obtained after sign-in
             
                }
            }
            catch (RequestFailedException ex)
            {
                Debug.LogException(ex);
                SetException(ex);
            }
        }

      
        void UpdateUI()
        {
            var statusBuilder = new StringBuilder();

            statusBuilder.AppendLine($"Player Accounts State: <b>{(PlayerAccountService.Instance.IsSignedIn ? "Signed in" : "Signed out")}</b>");
            statusBuilder.AppendLine($"Player Accounts Access token: <b>{(string.IsNullOrEmpty(PlayerAccountService.Instance.AccessToken) ? "Missing" : "Exists")}</b>\n");
            statusBuilder.AppendLine($"Authentication Service State: <b>{(AuthenticationService.Instance.IsSignedIn ? "Signed in" : "Signed out")}</b>");

            if (AuthenticationService.Instance.IsSignedIn)
            {
                m_SignOut.SetActive(true);
                statusBuilder.AppendLine(GetPlayerInfoText());
                statusBuilder.AppendLine($"PlayerId: <b>{AuthenticationService.Instance.PlayerId}</b>");
            }

            m_StatusText.text = statusBuilder.ToString();
            SetException(null);
        }

        string GetExternalIds(PlayerInfo playerInfo)
        {
            if (playerInfo.Identities == null)
            {
                return "None";
            }

            var sb = new StringBuilder();
            foreach (var id in playerInfo.Identities)
            {
                sb.Append(" " + id.TypeId);
            }

            return sb.ToString();
        }

        string GetPlayerInfoText()
        {
            return $"ExternalIds: <b>{m_ExternalIds}</b>";
        }

        void SetException(Exception ex)
        {
            m_ExceptionText.text = ex != null ? $"{ex.GetType().Name}: {ex.Message}" : "";
        }
    }
}
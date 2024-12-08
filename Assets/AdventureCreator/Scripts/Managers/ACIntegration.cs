using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using AC;
using System.Linq;
using System.Collections.Generic;
using Unity.Services.CloudSave.Models;
using Unity.Services.Authentication;

public class ACIntegration : MonoBehaviour
{
    private string expectedPlayerId; 

    async void Start()
    {
        await WaitForSignIn();

        // Check if the player is signed in first
        if (IsPlayerSignedIn())
        {
            Debug.Log("Player signed in successfully.");

            if (IsPlayerIdMatching())
            {
                Debug.Log("Player ID matches. Proceeding with save operations.");
                CheckForLocalSaves();
                DownloadAllCloudSaves();
            }
            else
            {
                Debug.Log("Player ID does not match. Skipping save operations.");
            }
        }
        else
        {
            Debug.Log("Account is new. Skipping save operations.");
        }
    }

    private bool IsPlayerSignedIn()
    {
        // Check if the player is signed in using Unity Authentication
        return AuthenticationService.Instance.IsSignedIn;
    }

    private bool IsPlayerIdMatching()
    {
        // Compare the current player ID with the expected one
        string currentPlayerId = AuthenticationService.Instance.PlayerId;
        Debug.Log($"Current Player ID: {currentPlayerId}, Expected Player ID: {expectedPlayerId}");

        // Optional: Load the expectedPlayerId from local storage if needed
        // expectedPlayerId = LoadExpectedPlayerIdFromLocalStorage();

        // Return true if they match
        return currentPlayerId == expectedPlayerId;
    }

    private async Task WaitForSignIn()
    {
        Debug.Log("Waiting for player to sign in...");
        while (!AuthenticationService.Instance.IsSignedIn)
        {
            await Task.Delay(10000); 
        }
    }

    private async void CheckForLocalSaves()
    {
        while (true)
        {
            await Task.Delay(40000);

            string latestSaveFile = GetLatestSaveFile();
            if (!string.IsNullOrEmpty(latestSaveFile))
            {
                Debug.Log("Local save found: " + latestSaveFile);
                await UploadLocalSave(latestSaveFile);
            }
        }
    }

    private string GetLatestSaveFile()
    {
        string saveDirectory = Application.persistentDataPath;

        var saveFiles = Directory.GetFiles(saveDirectory, "*.save");

        if (saveFiles.Length == 0)
        {
            return null;
        }

        return saveFiles.OrderByDescending(File.GetCreationTime).FirstOrDefault();
    }

    private async Task UploadLocalSave(string fullPath)
    {
        string fileName = Path.GetFileName(fullPath);

        if (File.Exists(fullPath))
        {
            byte[] fileData = File.ReadAllBytes(fullPath);

            try
            {
                await CloudSaveService.Instance.Files.Player.SaveAsync(fileName, fileData);
                Debug.Log($"Uploaded {fileName} to the cloud successfully.");
            }
            catch (CloudSaveException ex)
            {
                Debug.LogError($"Failed to upload {fileName}: {ex.Reason}");
            }
        }
        else
        {
            Debug.LogError($"Local save file not found: {fullPath}");
        }
    }

    public async void DownloadAllCloudSaves()
    {
        while (true)
        {
            await Task.Delay(40000);

            try
            {
                List<FileItem> cloudFiles = await CloudSaveService.Instance.Files.Player.ListAllAsync();

                foreach (var cloudFile in cloudFiles)
                {
                    string cloudFileName = cloudFile.Key;
                    Debug.Log($"Found cloud save file: {cloudFileName}");

                    await DownloadSaveFromCloud(cloudFileName);
                }
            }
            catch (CloudSaveException ex)
            {
                Debug.LogError($"Failed to list or download cloud files: {ex.Reason}");
            }
        }
    }

    public async Task DownloadSaveFromCloud(string fileName)
    {
        try
        {
            byte[] fileData = await CloudSaveService.Instance.Files.Player.LoadBytesAsync(fileName);

            if (fileData != null)
            {
                string saveDirectory = Application.persistentDataPath;
                string filePath = Path.Combine(saveDirectory, fileName);

                await File.WriteAllBytesAsync(filePath, fileData);

                Debug.Log($"Downloaded {fileName} and saved it locally at {filePath}");
            }
            else
            {
                Debug.LogError($"No data found for {fileName} in the cloud.");
            }
        }
        catch (CloudSaveException ex)
        {
            Debug.LogError($"Failed to download {fileName} from the cloud: {ex.Reason}");
        }
    }
}

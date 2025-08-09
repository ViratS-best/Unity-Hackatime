
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

namespace WakaTime {
   public class ClientManager {
	   public static void HeartBeat(string apiKey, string file, bool write = false) {
		   // Read config if not provided
		   string configApiKey = apiKey;
		   string configApiUrl = null;
		   ReadWakatimeConfig(ref configApiKey, ref configApiUrl);
		   if (string.IsNullOrEmpty(configApiKey)) {
			   Debug.LogError("No API key found in .wakatime.cfg or arguments.");
			   return;
		   }
		   if (string.IsNullOrEmpty(configApiUrl)) {
			   configApiUrl = "https://hackatime.hackclub.com/api/hackatime/v1";
		   }

		   // Prepare heartbeat data
		   var heartbeat = new HeartbeatData {
			   api_key = configApiKey,
			   entity = file,
			   type = "file",
			   category = write ? "save" : "edit",
			   project = Main.GetProjectName(),
			   plugin = WakaTimeConstants.PLUGIN_NAME,
			   time = (System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds
		   };

		   // Wrap in heartbeats array for WakaTime/Hackatime compatibility
		   var wrapper = new HeartbeatWrapper { heartbeats = new HeartbeatData[] { heartbeat } };
		   string json = JsonUtility.ToJson(wrapper);
		   var request = new UnityWebRequest(configApiUrl, "POST");
		   byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
		   request.uploadHandler = new UploadHandlerRaw(bodyRaw);
		   request.downloadHandler = new DownloadHandlerBuffer();
		   request.SetRequestHeader("Content-Type", "application/json");

		   var async = request.SendWebRequest();
		   async.completed += (op) => {
			   if (request.result != UnityWebRequest.Result.Success) {
				   Debug.LogError("Hackatime Error: " + request.error + "\n" + request.downloadHandler.text);
			   } else if (Main.IsDebug) {
				   Debug.Log("Hackatime Success: " + request.downloadHandler.text);
			   }
			   request.Dispose();
		   };
	   }

	   // Reads .wakatime.cfg from user home directory
	   private static void ReadWakatimeConfig(ref string apiKey, ref string apiUrl) {
		   try {
			   string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			   string configPath = Path.Combine(home, ".wakatime.cfg");
			   if (!File.Exists(configPath)) return;
			   string[] lines = File.ReadAllLines(configPath);
			   bool inSettings = false;
			   foreach (var line in lines) {
				   string trimmed = line.Trim();
				   if (trimmed.StartsWith("[") && trimmed.EndsWith("]")) {
					   inSettings = trimmed.Equals("[settings]", StringComparison.OrdinalIgnoreCase);
					   continue;
				   }
				   if (!inSettings || string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#")) continue;
				   int eq = trimmed.IndexOf('=');
				   if (eq < 0) continue;
				   string key = trimmed.Substring(0, eq).Trim();
				   string value = trimmed.Substring(eq + 1).Trim();
				   if (key.Equals("api_key", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(apiKey)) {
					   apiKey = value;
				   } else if (key.Equals("api_url", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(apiUrl)) {
					   apiUrl = value;
				   }
			   }
		   } catch (Exception ex) {
			   Debug.LogError("Failed to read .wakatime.cfg: " + ex.Message);
		   }
	   }

	   [System.Serializable]
	   private class HeartbeatWrapper {
		   public HeartbeatData[] heartbeats;
	   }
	   [System.Serializable]
	   private class HeartbeatData {
		   public string api_key;
		   public string entity;
		   public string type;
		   public string category;
		   public string project;
		   public string plugin;
		   public double time;
	   }
   }
}
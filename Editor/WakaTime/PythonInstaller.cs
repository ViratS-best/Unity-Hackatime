
using UnityEditor;
using System;
using UnityEngine;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

namespace WakaTime {
	public class PythonInstaller {

		static UnityWebRequest www = null;

		static Process installProcess = null;

		static string GetFileFolder() {
			return System.Environment.GetFolderPath (System.Environment.SpecialFolder.ApplicationData);
		}
		
		static string GetFilePath() {
			return GetFileFolder () + PythonManager.GetPythonFileName ();
		}
		
		static bool IsDownloaded() {
			return File.Exists (GetFilePath ());
		}

		static public void DownloadAndInstall() {
			if (!PythonManager.IsPythonInstalled ()) {
				if(!IsDownloaded()) {
					Download();
				} else {
					Install();
				}
			}
		}

		static public bool IsInstalling() {
			return IsDownloading () || installProcess != null;
		}


		   static public void Download () {
			   string url = PythonManager.GetPythonDownloadUrl ();
			   www = UnityWebRequest.Get(url);
			   www.SendWebRequest();
			   EditorApplication.update += WhileDownloading;
		   }


		   public static bool IsDownloading() {
			   return www != null && !www.isDone;
		   }


		   static void WhileDownloading () {
			   if (www == null) return;
			   EditorUtility.DisplayProgressBar ("Downloading Python", "Python is being downloaded", www.downloadProgress);
           
			   if (www.isDone) {
				   EditorApplication.update -= WhileDownloading;
				   DownloadCompleted ();
			   }
		   }


		   static void DownloadCompleted () {
			   EditorUtility.ClearProgressBar ();

			   if (Main.IsDebug) {
				   UnityEngine.Debug.Log ("Python downloaded: " + (www.downloadedBytes.ToString()));
			   }
			   string dir = System.Environment.GetFolderPath (System.Environment.SpecialFolder.ApplicationData);
			   string localFile = dir + PythonManager.GetPythonFileName ();

			   try {
				   File.WriteAllBytes(localFile, www.downloadHandler.data);
				   www.Dispose();
				   www = null;
			   } catch(Exception ex) {
				   if(Main.IsDebug) {
					   UnityEngine.Debug.LogError("Python download failed: " + ex.Message);
				   }
			   }

			   Install();
		   }

		static void Install() {
			string arguments = "/i \"" + GetFilePath() + "\"";
			arguments = arguments + " /norestart /qb!";

			try {
				var procInfo = new ProcessStartInfo
				{
					UseShellExecute = false,
					RedirectStandardError = true,
					FileName = "msiexec",
					CreateNoWindow = true,
					Arguments = arguments
				};
			
				installProcess = Process.Start(procInfo);
				installProcess.WaitForExit();
				installProcess.Close();

				installProcess = null;
			} catch(Exception ex) {
				if(Main.IsDebug) {
					UnityEngine.Debug.LogError("Python installation failed: " +  ex.Message);
				}
			}
		}
	}
}
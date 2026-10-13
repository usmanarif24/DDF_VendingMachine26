#if UNITY_ANDROID

using UnityEngine;
using UnityEditor.Android;
using System.IO;
using System.Text;

//-----------------------------------------------------------------------------
// Copyright 2012-2021 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Editor
{
	public class PostProcessBuild_Android : IPostGenerateGradleAndroidProject
	{
		public int callbackOrder { get { return 1; } }

		public void OnPostGenerateGradleAndroidProject( string path )
		{
			GradleProperty( path );
#if UNITY_6000_0_OR_NEWER
			// AVPro bundles full Guava, which already supplies ListenableFuture. Modern AndroidX
			// dependencies also pull in the standalone artifact, producing duplicate classes.
			if (File.Exists("Assets/AVProVideo/Runtime/Plugins/Android/guava-31.1-android.jar"))
			{
				string rootGradle = Path.Combine(path, "..", "build.gradle");
				const string marker = "// AVPro bundled Guava compatibility";
				if (File.Exists(rootGradle) && !File.ReadAllText(rootGradle).Contains(marker))
					File.AppendAllText(rootGradle, "\n" + marker + "\nallprojects {\n    configurations.configureEach {\n        exclude group: 'com.google.guava', module: 'listenablefuture'\n    }\n}\n");
			}
#endif
		}

		private void GradleProperty( string path )
		{
#if UNITY_2020_1_OR_NEWER || UNITY_2020_OR_NEWER
			// When using Unity 2020.1 and above it has been seen that the build process overly optimises which causes issues in the ExoPlayer library.
			// The legacy workaround is not supported by the Android Gradle plugin shipped with Unity 6.
			// Always remove stale copies, and only add the workaround for older Unity versions.

			Debug.Log("[AVProVideo] Post-processing Android project: patching gradle.properties");

			StringBuilder stringBuilder = new StringBuilder();

			// Path to gradle.properties
			string filePath = Path.Combine( path, "..", "gradle.properties" );

			if( File.Exists( filePath ) )
			{
				// Load in all the lines in the file
				string[] allLines = File.ReadAllLines( filePath );

				foreach( string line in allLines )
				{
					if( line.Length > 0 )
					{
						// Add everything except enableDexingArtifactTransform
						if ( !line.Contains( "android.enableDexingArtifactTransform" ) )
						{
							stringBuilder.AppendLine( line );
						}
					}
				}
			}

			// Android Gradle plugin 8.3+ removed this option; Unity 6 must use the default dexing pipeline.
#if !UNITY_6000_0_OR_NEWER
			stringBuilder.AppendLine( "android.enableDexingArtifactTransform=false" );
#endif

			// Write out the amended file
			File.WriteAllText( filePath, stringBuilder.ToString() );
#endif
		}
	}
}

#endif // UNITY_ANDROID

//
// Copyright 2014-2015 Amazon.com, 
// Inc. or its affiliates. All Rights Reserved.
// 
// Licensed under the AWS Mobile SDK For Unity 
// Sample Application License Agreement (the "License"). 
// You may not use this file except in compliance with the 
// License. A copy of the License is located 
// in the "license" file accompanying this file. This file is 
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, express or implied. See the License 
// for the specific language governing permissions and 
// limitations under the License.
//

using UnityEngine;
using System.Collections.Generic;
using Facebook.Unity;

using Amazon;
using Amazon.Runtime;
using Amazon.CognitoIdentity;
using System.Collections;
using System;
using System.Text;
using System.Security.Cryptography;

namespace AWSSDK.Examples
{

    public class CognitoSyncManagerSample : MonoBehaviour
    {

        void Awake()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            if (!FB.IsInitialized)
            {
                // Initialize the Facebook SDK
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                // Already initialized, signal an app activation App Event
                FB.ActivateApp();
            }
        }

        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
                // Continue with Facebook SDK
                // ...
            }
            else
            {
                Debug.Log("Failed to Initialize the Facebook SDK");
            }
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }

        public void LoginWithFacebook()
        {
            FB.LogOut();
            var perms = new List<string>() { "public_profile", "email" };
            FB.LogInWithReadPermissions(perms, AuthCallback);
        }

        private void AuthCallback(ILoginResult result)
        {
            if (FB.IsLoggedIn)
            {
                // AccessToken class will have session details
                var aToken = AccessToken.CurrentAccessToken.TokenString;
                CognitoLogin(aToken);
            }
            else
            {
                Debug.Log("User cancelled login");
            }
        }

        void CognitoLogin(string facebookToken)
        {
            Debug.Log(facebookToken);
            string cognitoPool = "us-east-1:53dd5506-b8ba-4543-b883-c74c206c1f4d";
            RegionEndpoint cognitoRegion = RegionEndpoint.USEast1;
            CognitoAWSCredentials credentials = new CognitoAWSCredentials(cognitoPool, cognitoRegion);
            credentials.AddLogin("graph.facebook.com", facebookToken);
            credentials.GetCredentialsAsync(CognitoGetCredentialsCallback, null);
        }

        private void CognitoGetCredentialsCallback(AmazonCognitoIdentityResult<ImmutableCredentials> result)
        {
            // if no exception, start the HTTP Get request
            //if (result.Exception == null)
            //  StartCoroutine(AuthenticatedGet((ImmutableCredentials)result.Response));
            //else
            //  Debug.LogException(result.Exception);
        }

        private static byte[] ToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str.ToCharArray());
        }

        private static string HexEncode(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static byte[] Hash(byte[] bytes)
        {
            return SHA256.Create().ComputeHash(bytes);
        }

        private static byte[] HmacSha256(string data, byte[] key)
        {
            return new HMACSHA256(key).ComputeHash(ToBytes(data));
        }

        // https://gist.github.com/jesusnoseq/da3b259dcef697fc310be878800d1c33
        void AuthenticatedGet(ImmutableCredentials cognitoCredentials)
        {
            // ************* REQUEST VALUES *************
            string accessKey = cognitoCredentials.AccessKey;
            string secretKey = cognitoCredentials.SecretKey;
            string securityToken = cognitoCredentials.Token;

            string algorithm = "AWS4-HMAC-SHA256";
            string method = "GET";
            string service = "execute-api";
            string serviceForSigning = "apigateway";
            string contentType = "application/json";
            string expires = "900";
            string amzDate = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            string dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");

        }


        void Start()
        {
            UnityInitializer.AttachToGameObject(this.gameObject);
            AWSConfigs.HttpClient = AWSConfigs.HttpClientOption.UnityWebRequest;
        }

        void OnGUI()
        {
            GUI.color = Color.gray;
            GUILayout.BeginArea(new Rect(Screen.width * 0.2f, 0, Screen.width - Screen.width * 0.2f, Screen.height));
            GUILayout.Space(20);

            GUILayout.Space(20);

            if (GUILayout.Button("Connect to Facebook", GUILayout.MinHeight(20), GUILayout.Width(Screen.width * 0.6f)))
            {
                LoginWithFacebook();
            }
            GUILayout.EndArea();

        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamKit2;
using System.IO;

namespace OgreBot1
{
    class Program
    {
        static string password, username;
        static SteamClient steamClient;
        static SteamUser steamUser;
        static CallbackManager callbackManager;
        static bool isRunning = false;
        static string authenticationCode;
        static SteamFriends steamFriends;
        static SteamTrading steamTrading;
         
        

        static void Main(string[] args)
        {
            Console.Title = "OgreBot1";
            Console.WriteLine("Ctrl+C to quit");

            //Console.Write("Username: ");
            //username = Console.ReadLine();
            username = "USERNAME";

            //Console.Write("Password: ");
            //password = Console.ReadLine();
            password = "PASSWORD";

            LogIn();
        }
        static void LogIn()
        {
            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);
            steamUser = steamClient.GetHandler<SteamUser>();
            steamFriends = steamClient.GetHandler<SteamFriends>();
            steamTrading = steamClient.GetHandler<SteamTrading>();
            new Callback<SteamClient.ConnectedCallback>(OnConnect,callbackManager);
            new Callback<SteamUser.LoggedOnCallback>(OnLoggedOn, callbackManager);
            new Callback<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth, callbackManager);
            new Callback<SteamClient.DisconnectedCallback>(OnDisconnected, callbackManager);
            new Callback<SteamUser.AccountInfoCallback>(OnAccountInfo, callbackManager);
            new Callback<SteamFriends.FriendMsgCallback>(OnChatMessage, callbackManager);
            new Callback<SteamFriends.FriendsListCallback>(OnFriendInvite, callbackManager);
            new Callback<SteamTrading.TradeProposedCallback>(OnTradeOffer, callbackManager);
            new Callback<SteamTrading.SessionStartCallback>(OnTradeWindow, callbackManager);
            new Callback<SteamTrading.TradeResultCallback>(OnTradeResult, callbackManager);
            

            isRunning = true;

            Console.WriteLine("Attempting to connect to steam...");

            steamClient.Connect();
            
            while(isRunning)
            {
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
            Console.ReadKey();
        }
        static void OnConnect(SteamClient.ConnectedCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Error logging in: {0}", callback.Result);
                isRunning = false;
                return;
            }
            Console.WriteLine("{0} connected", username);
            byte[] sentryHash = null;
            if(File.Exists("sentry.bin"))
            {
                byte[] sentryFile = File.ReadAllBytes("sentry.bin");
                sentryHash = CryptoHelper.SHAHash(sentryFile);
            }
            steamUser.LogOn(new SteamUser.LogOnDetails 
            { 
                Username = username, 
                Password = password, 
                AuthCode = authenticationCode,
                SentryFileHash = sentryHash,
            });
            
        }
        static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if(callback.Result == EResult.AccountLogonDenied)
            {
                Console.WriteLine("Account is SteamGuard protected.");
                Console.Write("Enter code sent to email at {0}: ", callback.EmailDomain);
                authenticationCode = Console.ReadLine();

                return;
            }
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Error logging in: {0}",callback.Result);
                isRunning = false;
                return;
            }
            Console.WriteLine("{0} logged in", username);
            //Console.ReadKey();
            //Environment.Exit(0);
        }
        static void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            Console.WriteLine("Updating sentry file...");

            byte[] sentryHash = CryptoHelper.SHAHash(callback.Data);
            File.WriteAllBytes("sentry.bin", callback.Data);

            steamUser.SendMachineAuthResponse(new SteamUser.MachineAuthDetails
                {
                    JobID = callback.JobID,
                    FileName = callback.FileName,
                    BytesWritten = callback.BytesToWrite,
                    FileSize = callback.Data.Length,
                    Offset = callback.Offset,
                    Result = EResult.OK,
                    LastError = 0,
                    OneTimePassword = callback.OneTimePassword,
                    SentryFileHash = sentryHash,
                });
            Console.WriteLine("Done.");
        }
        static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("{0} disconnected from steam, reconnecting in 3 sec...", username);
            TimeSpan.FromSeconds(3);
            steamClient.Connect();
        }
        static void OnAccountInfo(SteamUser.AccountInfoCallback callback)
        {
            steamFriends.SetPersonaState(EPersonaState.Online);
        }
        static void OnChatMessage(SteamFriends.FriendMsgCallback callback)
        {
            if (callback.EntryType == EChatEntryType.ChatMsg)
            {
                steamFriends.SendChatMessage(callback.Sender, EChatEntryType.ChatMsg, "Hello");
                steamTrading.Trade(callback.Sender);
            }
        }
        static void OnFriendInvite(SteamFriends.FriendsListCallback callback)
        {
            TimeSpan.FromSeconds(3);
            foreach(var friend in callback.FriendList)
            {
                if(friend.Relationship == EFriendRelationship.RequestRecipient)
                {
                    steamFriends.AddFriend(friend.SteamID);
                }
            }
        }
        static void OnTradeOffer(SteamTrading.TradeProposedCallback callback)
        {
            
            TimeSpan.FromSeconds(3);
            steamTrading.RespondToTrade(callback.TradeID, true);
            
        }    
        static void OnTradeWindow(SteamTrading.SessionStartCallback callback)
        { 

        }
        static void OnTradeResult(SteamTrading.TradeResultCallback callback)
        {
            
        }
                   
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using HQMEditorDedicated;
using System.Threading.Tasks;
using PubstarsDtos;

namespace PubstarsClient
{
    class Program
    {
        public static void Main(string[] args)
        {
            RankedGameReport g = new RankedGameReport()
            {
                RedScore = 10,
                BlueScore = 11,
                WinningTeam = "Blue",
                Date = DateTime.UtcNow,
                ServerName = "testServer"
            };

            g.PlayerStats.Add(new RankedGameReport.PlayerStatLine()
            {
                Name = "omaha",
                Goals = 5,
                Assists = 2,
                Team = "Blue",
                Leaver = false

            });         

            RemoteApi.SendGameResult(g);
            

            Console.WriteLine("Looking for server...");
            while (!MemoryEditor.Init()) { }
            Console.WriteLine("Server found.");

            Console.WriteLine("Reading user data...");
            RemoteApi.GetUserData();
            Console.WriteLine("done.");

            CommandListener cmdListener = new CommandListener(Chat.MessageCount);
            Chat.RecordCommandSource();            

            RankedGame game = new RankedGame();
            Chat.FlushLastCommand();

            AppDomain.CurrentDomain.UnhandledException += CrashReporter;

            Thread removeTresspassers = new Thread(game.RemoveTrespassers);
            removeTresspassers.Start();


            while (true)
            {
                if (LoginManager.LoggedInPlayers.Count > 0)
                    LoginManager.RemoveLoggedOutPlayers();

                if (game.InProgress)
                {
                    if (GameInfo.IsGameOver)
                    {
                        game.EndGame(true);
                    }
                    if (CheckMercy() && !game.IsEndingDueToMercyRule)
                    {
                        game.IsEndingDueToMercyRule = true;
                        Chat.SendMessage("---------------------------------------------------");
                        Chat.SendMessage("  Game is ending due to mercy rule.");
                        Chat.SendMessage("---------------------------------------------------");
                        GameInfo.Period = 3;
                        GameInfo.GameTime = new TimeSpan(0, 0, 0, 1);
                    }
                }
                else
                {
                    if (LoginManager.LoggedInPlayers.Count >= Util.MIN_PLAYER_COUNT && !game.StartingGame && GameInfo.Period < 4)
                    {
                        game.StartGameTimer();
                        Thread.Sleep(Util.MAINTHREAD_SLEEP);
                        Chat.SendMessage("---------------------------------------------------");
                        Chat.SendMessage("     Required player count reached.");
                        Chat.SendMessage("     Game will start in " + Util.GAME_START_TIMER + " seconds.");
                        Chat.SendMessage("---------------------------------------------------");
                    }
                }

                Command cmd = cmdListener.NewCommand();
                if (cmd != null)
                {
                    LoginManager.HandleNewLogins(cmd);
                    UtilCommandHandler.HandleUtilCommand(cmd);

                    if (cmd.Cmd == "start" && cmd.Sender.IsAdmin)
                    {
                        game.StartGame();
                    }
                    else if (cmd.Cmd == "end" && cmd.Sender.IsAdmin)
                    {
                        game.EndGame(false);
                    }
                    else if (cmd.Cmd == "info" && (!game.InProgress || game.StartingGame))
                    {
                        InfoMessage();
                    }
                    Chat.FlushLastCommand();
                }
                Thread.Sleep(Util.MAINTHREAD_SLEEP);
            }

        }

        static void InfoMessage()
        {
            Chat.SendMessage("             Logged in players: "+LoginManager.LoggedInPlayers.Count + " / "+Util.MIN_PLAYER_COUNT);
            Chat.SendMessage("        Type /join <yourpassword> to play");
            Chat.SendMessage("        Create an account at r/hqmgames "); 
        }

        static void CrashReporter(object sender, UnhandledExceptionEventArgs args)
        {
            Chat.SendMessage("HQMRanked crashed.");
            Chat.SendMessage(args.ExceptionObject.ToString());
        }

        public static bool CheckMercy()
        {
            byte[] score = MemoryEditor.ReadBytes(0x018931F8, 8);
            int redScore = score[0];
            int blueScore = score[4];
            return (Math.Abs(redScore - blueScore) >= Util.MERCY_RULE_DIFF);
        }

        
    }
}

﻿using LodeNaVode.Data;
using LodeNaVode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Linq;

namespace LodeNaVode.Controllers
{
    public class LobbyController : Controller
    {
        private LobbyDbContext _lobbyDatabase;

        public LobbyController(LobbyDbContext dbContext)
        {
            _lobbyDatabase = dbContext;
        }

        [HttpGet]
        public IActionResult Create(Lobby model)
        {
            string lobbyOwnerId = HttpContext.Session.GetString("playerid");

            var lobbyOwner = _lobbyDatabase.Players
                .Where(p => p.PlayerCookie == lobbyOwnerId)
                .FirstOrDefault();

            if (lobbyOwner == null)
            {
                RedirectToAction("Index");
            }
            else
            {
                List<Player> players = new List<Player> { lobbyOwner };
                Lobby newLobby = new Lobby() { Gamemode = "normal", Owner = lobbyOwnerId, Players = players , Active = true };

                _lobbyDatabase.Lobbies.Add(newLobby);
                _lobbyDatabase.SaveChanges();
            }


            return RedirectToAction("Lobby");
        }

        [HttpGet]
        public IActionResult Join(string lobbyId)
        {
            if (lobbyId == null || lobbyId.Trim().Length == 0)
                return Redirect("/Lobby/Index");

            int id = 0;
            try
            {
                id = Convert.ToInt32(lobbyId);
            }
            catch
            {
                return Redirect("/Lobby/Index");
            }

            string playerCookie = HttpContext.Session.GetString("playerid");
            if (_lobbyDatabase.Lobbies.Any(l => l.LobbyId == id))
            {
                Player player = _lobbyDatabase.Players.Where(p => p.PlayerCookie == playerCookie).First();
                var lobbies = _lobbyDatabase.Lobbies;
                var correctLobbies = lobbies.Where(l => l.LobbyId == id);
                var playersOfLobby = correctLobbies.First().Players;
                playersOfLobby.Add(player);
                _lobbyDatabase.SaveChanges();
                return RedirectToAction("Lobby");
            }
            return RedirectToAction("JoinLobby", "Home");
        }

        public IActionResult Lobby()
        {
            Player? playercheck = _lobbyDatabase.Players.Where(p => p.PlayerCookie == HttpContext.Session.GetString("playerid")).FirstOrDefault();
            Lobby currentLobby = _lobbyDatabase.Lobbies.Where(l => l.Players.Contains(playercheck)).First();
            ViewData["lobbyOwner"] = currentLobby.Owner;
            ViewData["currentUser"] = playercheck.PlayerCookie;
            return View();
        }

        public IActionResult Index()
        {
            string? from = HttpContext.Session.GetString("from");
            string? playerName = HttpContext.Session.GetString("playername");
            if (playerName != null && playerName.Trim() != "")
            {
                if (!_lobbyDatabase.Players.Any(p => p.PlayerName == playerName))
                {
                    string? playerCookie = HttpContext.Session.GetString("playerid");
                    Player player = new Player() { PlayerCookie = playerCookie, PlayerName = playerName };
                    _lobbyDatabase.Players.Add(player);
                    _lobbyDatabase.SaveChanges();
                    return View();
                }
                else 
                {
                    if (from == "Lobby")
                    {
                        HttpContext.Session.Remove("from");
                        return View();
                    }

                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            else
                return RedirectToAction("Index", "Home");

        }

        public IActionResult Leave(string from)
        {
            HttpContext.Session.SetString("from", from);
            Player? playercheck = _lobbyDatabase.Players.Where(p => p.PlayerCookie == HttpContext.Session.GetString("playerid")).FirstOrDefault();
            Lobby currentLobby = _lobbyDatabase.Lobbies.Where(l => l.Players.Contains(playercheck)).First();
            if (playercheck != null)
            {
                Lobby lobbyWithPlayer = _lobbyDatabase.Lobbies.Where(l => l.Players.Contains(playercheck)).First();
                lobbyWithPlayer.Players.Remove(playercheck);

                if (playercheck.PlayerCookie == currentLobby.Owner) 
                {
                    if (currentLobby.Players.IsNullOrEmpty()) 
                    {
                        currentLobby.Active = false;
                        currentLobby.Owner = null;
                        _lobbyDatabase.SaveChanges();
                    }
                    else 
                    {
                        currentLobby.Owner = currentLobby.Players.First().PlayerCookie;
                        _lobbyDatabase.SaveChanges();
                    }
                } 
                else
                    _lobbyDatabase.SaveChanges();

                return RedirectToAction("Index", "Lobby");
            }
            else
                throw new NotImplementedException();
        }
    }
}
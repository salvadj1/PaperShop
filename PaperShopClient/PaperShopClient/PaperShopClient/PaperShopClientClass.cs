using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RustBuster2016;
using UnityEngine;

namespace PaperShopClient
{
    public class PaperShopClientClass : RustBuster2016.API.RustBusterPlugin
    {
        public override string Name { get { return "PaperShopClient"; } }
        public override string Author { get { return " by salva/juli"; } }
        public override Version Version { get { return new Version("1.0"); } }

        public Mono MonoClass;
        public GameObject monoClassLoad;


        public static PaperShopClientClass Instance;

        public override void Initialize()
        {
            Instance = this;
            if (this.IsConnectedToAServer)
            {
                RustBuster2016.API.Hooks.OnRustBusterClientConsole += OnRustBusterClientConsole;
                monoClassLoad = new GameObject();
                MonoClass = monoClassLoad.AddComponent<Mono>();
                UnityEngine.Object.DontDestroyOnLoad(monoClassLoad);

                return;
            }
        }
        public override void DeInitialize()
        {
            RustBuster2016.API.Hooks.OnRustBusterClientConsole -= OnRustBusterClientConsole;
            if (monoClassLoad != null) UnityEngine.Object.DestroyImmediate(monoClassLoad);
        }
        public void OnRustBusterClientConsole(string message)
        {
            if (message.Contains("MensajeDeLaTienda"))
            {
                //string[] partir = message.ToString().Split(new char[] { "\n" });
                string[] split = message.Split('-');
                string mensaje = split[1];

                //List<string> archivos = message.Split(new[] { "\n" },
                //StringSplitOptions.RemoveEmptyEntries).ToList();
                
                //int count = listado.Count();

                Mono.MessageShop = mensaje;
                Mono.Show = true;
                
                Timer1(60 * 1000, null).Start();
            }
        }
        public TimedEvent Timer1(int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = new TimedEvent(timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += DataStoreSave;
            return timedEvent;
        }
        public void DataStoreSave(TimedEvent e)
        {
            e.Kill();
            Mono.Show = false;
        }
    }
}

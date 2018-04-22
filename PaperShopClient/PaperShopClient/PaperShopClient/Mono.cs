using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PaperShopClient
{
    public class Mono : MonoBehaviour
    {
        public static bool Show = false;
        public static string MessageShop = "";

        public void OnGUI()
        {
            if (Show)
            {
                GUI.Box(new Rect(10, 200, 200, 250), string.Format("<b><color=#31B404>PaperShop Price List</color></b>"));
                GUI.Label(new Rect(20, 220, 180, 250), string.Format("<b> " + MessageShop + " </b>"));
            }
        }
    }
}

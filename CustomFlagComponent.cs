using BepInEx.Logging;
using System;
using System.Collections;
using UnityEngine;
using static CustomShipFlags.Plugin;

#pragma warning disable CS8632
namespace CustomShipFlags
{
    public class CustomFlagComponent : MonoBehaviour, Interactable, TextReceiver, Hoverable
    {
        private ZNetView m_nview;
        private Renderer renderer;
        private BoxCollider collider;

        private void Awake()
        {
            m_nview = transform.parent.parent.parent.parent.parent.gameObject.GetComponent<ZNetView>();
            m_nview.Register("SetFlagUrl", new Action<long>(RPC_SetCustomFlag));
            renderer = GetComponent<Renderer>();
            collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new(0, -6.4f, 0);
            collider.size = new(-0.23f, 21, 4);

            _self.Config.ConfigReloaded += (_, _) => { LoadSavedTextureFromURL(); };
        }

        private void Start()
        {
            LoadSavedTextureFromURL();
        }

        public bool Interact(Humanoid human, bool hold, bool alt)
        {
            if(Plugin.useOnlyServerFlag)
            {
                human.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
                return false;
            }

            if(hold)
            {
                return false;
            }
            if(!PrivateArea.CheckAccess(transform.position, 0f, true, false))
            {
                human.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
                return true;
            }
            TextInput.instance.RequestText(this, "Enter Texture Url", 9999);
            return true;
        }
        public string GetText()
        {
            if(!m_nview) return "";
            return m_nview.GetZDO().GetString("TextureUrl", "");
        }
        public void SetText(string text)
        {
            _self.Debug($"SetText {text}");
            if(!m_nview.IsValid())
            {
                return;
            }
            m_nview.GetZDO().Set("TextureUrl", text);
            m_nview.InvokeRPC("SetFlagUrl");
        }

        void RPC_SetCustomFlag(long sender)
        {
            string url = GetText();
            if(!m_nview.IsValid() || !m_nview.IsOwner())
            {
                return;
            }
            SetTextureFromURL(url);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) { return false; }
        public string GetHoverText()
        {
            if(!Plugin.useOnlyServerFlag) return "Set Flag Url";
            else
            {
                if(Player.m_localPlayer.InDebugFlyMode()) return "Set Flag Url";
                else return "";
            }
        }
        public string GetHoverName()
        {
            return "";
        }


        public void SetTextureFromURL(string url)
        {
            string urlFormated = url.Replace(" ", "");
            if(string.IsNullOrEmpty(urlFormated) || string.IsNullOrWhiteSpace(urlFormated)) return;
            StartCoroutine(SetTextureFromURLIEnumerator(urlFormated, renderer));
        }

        private bool isUrlCorrect(string url) => !string.IsNullOrEmpty(url) && !string.IsNullOrWhiteSpace(url) && (url.EndsWith(".jpg") || url.EndsWith(".png")|| url.EndsWith(".jpeg"));

        public void LoadSavedTextureFromURL()
        {
            string url = GetText().Replace(" ", "");
            if(useOnlyServerFlag)
            {
                url = serverFlagUrl;
            }
            else
            {
                if(!isUrlCorrect(url))
                {
                    url = serverFlagUrl;
                    url = url.Replace(" ", "");
                }
                if(isUrlCorrect(url))
                {
                    _self.Debug(LogLevel.Warning, "The server flag is missing, the vanilla version will be used.");
                    return;
                }
            }

            m_nview.GetZDO().Set("TextureUrl", url);

            StartCoroutine(SetTextureFromURLIEnumerator(url, renderer));
        }
        private IEnumerator SetTextureFromURLIEnumerator(string url, Renderer renderer)
        {
            Plugin._self.Debug(LogLevel.Info, "Loading...");
            WWW wwwLoader = new(url);
            yield return wwwLoader;

            renderer.material.color = Color.white;
            renderer.material.mainTexture = wwwLoader.texture;
            Plugin._self.Debug(LogLevel.Info, "Loaded");
        }
    }
}

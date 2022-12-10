using BepInEx.Logging;
using System;
using System.Collections;
using UnityEngine;

#pragma warning disable CS8632
namespace CustomShipFlags
{
    public class CustomFlagComponent : MonoBehaviour, Interactable, TextReceiver, Hoverable
    {
        private ZNetView? m_nview;
        private Renderer renderer;
        private BoxCollider collider;

        private void Awake()
        {
            m_nview = transform.parent.parent.parent.parent.parent.gameObject.GetComponent<ZNetView>();
            m_nview.Register("SetFlagUrl", new Action<long, string>(RPC_SetCustomFlag));
            renderer = GetComponent<Renderer>();
            collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new(0, -6.4f, 0);
            collider.size = new(-0.23f, 21, 4);

            Plugin._self.Config.ConfigReloaded += (_, _) => { LoadSavedTextureFromURL(); };


            LoadSavedTextureFromURL();
        }

        public bool Interact(Humanoid human, bool hold, bool alt)
        {
            if (Plugin.useOnlyServerFlag)
            {
                human?.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
                return false;
            }

            if (hold)
            {
                return false;
            }
            if (!PrivateArea.CheckAccess(transform.position, 0f, true, false))
            {
                human?.Message(MessageHud.MessageType.Center, "$piece_noaccess", 0, null);
                return true;
            }
            TextInput.instance.RequestText(this, "Enter Texture Url", 9999);
            return true;
        }
        public string GetText()
        {
            return m_nview?.GetZDO()?.GetString("TextureUrl", "");
        }
        public void SetText(string text)
        {
            if (!m_nview.IsValid())
            {
                return;
            }
            m_nview?.InvokeRPC("SetFlagUrl", new object[]
            {
                text
            });
        }
        void RPC_SetCustomFlag(long sender, string url)
        {
            if (!m_nview.IsValid() || !m_nview.IsOwner())
            {
                return;
            }
            if (GetText() == url)
            {
                return;
            }
            m_nview.GetZDO().Set("TextureUrl", url);
            SetTextureFromURL(url);
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item) { return false; }
        public string GetHoverText()
        {
            return "Set Flag Url";
        }
        public string GetHoverName()
        {
            return "";
        }


        public void SetTextureFromURL(string url)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url)) return;
            StartCoroutine(SetTextureFromURLIEnumerator(url, renderer));
        }
        public void LoadSavedTextureFromURL()
        {
            string url;
            if (Plugin.useOnlyServerFlag)
            {
                url = Plugin.serverFlagUrl;
            }
            else
            {
                url = GetText();
                if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url))
                {
                    url = Plugin.serverFlagUrl;
                }
                if (string.IsNullOrEmpty(url) || string.IsNullOrWhiteSpace(url))
                {
                    Plugin._self.Debug(LogLevel.Warning, "The server flag is missing, the vanilla version will be used.");
                    return;
                }
            }

            if (url != GetText())
            {
                m_nview?.GetZDO()?.Set("TextureUrl", url);
            }

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

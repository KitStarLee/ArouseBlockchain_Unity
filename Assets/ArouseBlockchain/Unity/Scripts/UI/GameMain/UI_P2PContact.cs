using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using ArouseBlockchain.Common;
using ArouseBlockchain.P2PNet;
using NBitcoin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ArouseBlockchain.Common.AUtils;
using static ArouseBlockchain.P2PNet.NetNodePeer;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace ArouseBlockchain.UI
{
    /// <summary>
    /// P2P 对等端搜索、对接、通讯、同步的UI控制界面
    /// </summary>
    public class UI_P2PContact : UI_PagePopup
    {
        public Button butt_seach;

        public RectTransform content_root;

        public UserNodeListPart orgin_NodeConnPart;
        public UserMessageListPart orgin_MessagePart;

        public UserListColor OnlineColor;
        public UserListColor OfflineColor;

        public List<UserListPart> UserListParts = new List<UserListPart>();

        // Start is called before the first frame update
      

        public override void Init(RectTransform rect)
        {
            UIManager.P2PSync.OnPeerFind += OnPeerFind;
            UIManager.P2PSync.OnPeerLoss += OnPeerLoss;
            orgin_NodeConnPart._childroot.gameObject.SetActive(false);
            orgin_MessagePart._childroot.gameObject.SetActive(false);

            UserListParts.Add(orgin_NodeConnPart);
            UserListParts.Add(orgin_MessagePart);
           // SearchValidNode();
        }

        private void OnApplicationQuit()
        {
            UIManager.P2PSync.OnPeerFind -= OnPeerFind;
            UIManager.P2PSync.OnPeerLoss -= OnPeerLoss;
        }

      

        // Update is called once per frame
        void Update()
        {

        }

        void OnPeerFind(NodePeerNet nodePeer) {
            var freePart = UserListParts.Find(
                x=> !x._childroot.gameObject.activeSelf && (x is UserNodeListPart)
            );
            if(freePart == null) {
                freePart = new UserNodeListPart();
            }
            ALog.Log("找到一个Peer，创建UI中....");

            var tran = freePart._childroot;
            tran.SetParent(orgin_NodeConnPart._childroot, false);
            tran.gameObject.SetActive(true);
            freePart.InitUI(nodePeer.NodePeer);
            
           // nodePeer.Peer.SetListener.
            UserListParts.Add(freePart);
        }

        void OnPeerLoss(NodePeerNet nodePeer)
        {
            var onlinePart = UserListParts.Find(
               x => x._NodePeer.address_public == nodePeer.NodePeer.address_public
              // && x._childroot.gameObject.activeSelf
               && (x is UserNodeListPart)
           );

            ALog.Log("丢失一个Peer，关闭UI中....");
            if (onlinePart == null)
            {
                var tran = onlinePart._childroot;
                tran.gameObject.SetActive(false);
            }
            else {
                ALog.Log("错误，这里没有这个Peer,却提示我丢失了Peer {0} ", nodePeer.Peer.Remote);
            }
        }

        public void SearchValidNode()
        {
            UIManager.P2PSync.SearchValidNode();
        }
        public void StopSearchNode()
        {
            UIManager.P2PSync.StopSearchNode();
        }

      

        [Serializable]
        public struct GradientColor {
            public Color Color1;
            public Color Color2;
        }
        [Serializable]
        public struct UserListColor {
          
            public GradientColor bgColor;
            public Color statusDotColor;
        }

        public abstract class UserListPart {
            public NodePeer _NodePeer;

            public RectTransform _childroot;
            public UltimateClean.Gradient _Background;

            public RectTransform _Status;

            public TextMeshProUGUI _TimeDataText;

            public virtual void InitUI(NodePeer _Node) {

                _NodePeer = _Node;
                var timer = string.Format("{0:HH:mm}", AUtils.ToDateTimeByUTCUnix(_NodePeer.report_reach, true));
                _TimeDataText.text = timer;
            }
        }

        [Serializable]
        public class UserNodeListPart : UserListPart
        {
           // public RectTransform _Avatar;
            public RectTransform _BlockHeight;

            public TextMeshProUGUI _AddressText;
            public TextMeshProUGUI _InfoText;

            public Image _IsRelayImage;

            public override void InitUI(NodePeer _Node)
            {
                base.InitUI(_Node);
                IPEndPoint address = Net.FindRightNetAddress(_Node);
                _AddressText.text = address.ToString();
                _InfoText.text = "区块高度：" + _Node.node_state.height;
                _IsRelayImage.gameObject.SetActive(_Node.is_bootstrap);
            }

            void DownBlock()
            {
                //NetBlock.BlockClient blockClient = new NetBlock.BlockClient();

                //blockClient.DownCRequest(block); //给没有链接的Peer广播块
            }
        }

        [Serializable]
        public class UserMessageListPart : UserListPart
        {
            public RectTransform _Avatar;
            public RectTransform _MessageAmount;

            public TextMeshProUGUI _NameText;
            public TextMeshProUGUI _MessageText;

            public override void InitUI(NodePeer _Node)
            {
                base.InitUI(_Node);
            }
        }

    }
}
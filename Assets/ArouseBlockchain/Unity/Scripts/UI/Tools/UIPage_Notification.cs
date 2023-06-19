using System.Collections;
using System.Collections.Generic;
using ArouseBlockchain.UI;
using UltimateClean;
using UnityEngine;

public class UIPage_Notification : MonoBehaviour, IUIPage
{
    public GameObject Prefab;
    public Canvas Canvas;

    public NotificationType Type;
    public NotificationPositionType Position;

    public float Duration;
    //public string Title;
    //public string Message;

    private NotificationQueue queue;

    
    //public void Init(RectTransform rect)
    //{
    //    image = GetComponent<Image>();
    //}

    private void Start()
    {
        queue = FindObjectOfType<NotificationQueue>();
    }

    public void Launch(string Title, string Message)
    {
        if (queue != null)
        {
            queue.EnqueueNotification(Prefab, Canvas, Type, Position, Duration, Title, Message);
        }
        else
        {
            var go = Instantiate(Prefab);
            go.transform.SetParent(Canvas.transform, false);

            var notification = go.GetComponent<Notification>();
            notification.Launch(Type, Position, Duration, Title, Message);
        }
    }
}

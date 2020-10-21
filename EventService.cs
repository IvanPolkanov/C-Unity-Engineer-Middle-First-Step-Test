using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace EventService
{

    public static class ListExtensions
    {
        public static string ConvertToPostData(this List<EventService.ServiceEvent> serviceEvents)
        {
            lock (serviceEvents)
            {
                string data = "{ \n \"events\": [ \n";
                int eventsCount = serviceEvents.Count;

                for (var i = 0; i < eventsCount - 2; i++)
                {
                    data += "{" + string.Format(" \n \"type\": \"{0}\", ", serviceEvents[i].type);
                    data += string.Format("\n \"data\": \"{0}\", \n ", serviceEvents[i].data);
                }

                data += "{" + string.Format("\n \"type\": \"{0}\", ", serviceEvents[eventsCount - 1].type);
                data += string.Format("\n \"data\": \"{0}\" \n ", serviceEvents[eventsCount - 1].data) + "}";
                data += "\n ] \n}";

                return data;
            }
        }
    }

    public class EventService : MonoBehaviour
    {
        public class ServiceEvent
        {
            public string type;
            public string data;

            public ServiceEvent(string type, string data)
            {
                this.type = type;
                this.data = data;
            }
        }


        private List<ServiceEvent> _serviceEvents = new List<ServiceEvent>();
        private List<ServiceEvent> _tempServiceEvents = new List<ServiceEvent>();//used when data sending

        public string ServerUrl
        {
            get { return _serverUrl; }
            set { _serverUrl = value; }
        }

        public float CooldownBeforeSend
        {
            get { return _cooldownBeforeSend; }
            set { _cooldownBeforeSend = value > 0 ? value : 0; }
        }

        private float _cooldownBeforeSend = 10.0f;
        private string _serverUrl = "notSure.com";

        private bool _sendingData = false;

        public void TrackEvent(string type, string data)
        {
            if (_sendingData)
            {
                _tempServiceEvents.Add(new ServiceEvent(type, data));
            }
            else
            {
                if (_tempServiceEvents.Count != 0)
                {
                    _serviceEvents.AddRange(_tempServiceEvents);
                    _tempServiceEvents.Clear();
                }
                _serviceEvents.Add(new ServiceEvent(type, data));
            }
        }

        private void Start()
        {
            StartCoroutine(PostData());
        }

        private IEnumerator PostData()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(_cooldownBeforeSend);
                yield return StartCoroutine(Upload());
            }
        }

        private IEnumerator Upload()
        {
            _sendingData = true;
            string data = _serviceEvents.ConvertToPostData();
            _serviceEvents.Clear();

            UnityWebRequest www = UnityWebRequest.Post(_serverUrl, data);
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
            }
            else
            {
                if (www.responseCode == 200)
                {
                    Debug.Log("Data has been uploaded!");
                }
                else
                {
                    Debug.Log("Data hasn't been uploaded! \n Response code: "+ www.responseCode);
                }
            }
            _sendingData = false;
        }
    }

}

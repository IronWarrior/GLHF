using GLHF;
using GLHF.Transport;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField]
    Button hostButton, clientButton;

    [SerializeField]
    TMP_InputField hostPortInput, clientPortInput, addressInput;

    [SerializeField]
    Config config;

    [SerializeField]
    Runner runnerPrefab;

    private void Awake()
    {
        hostButton.onClick.AddListener(() => Host());
        clientButton.onClick.AddListener(() => Client());
    }

    private void Host()
    {
        var runner = Instantiate(runnerPrefab);

        ITransport transport = new TransportLiteNetLib();
        var transporter = new Transporter(transport);

        int port = Convert.ToInt32(hostPortInput.text);

        runner.Host(port, config, transporter);
        runner.StartGame();
    }

    private void Client()
    {
        var runner = Instantiate(runnerPrefab);

        ITransport transport = new TransportLiteNetLib();
        var transporter = new Transporter(transport);

        int port = Convert.ToInt32(hostPortInput.text);

        runner.Join(port, addressInput.text, config, transporter);
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;

public class GameMgr : MonoBehaviour
{
    static GameMgr instance = null;
    static public GameMgr Instance { get { return instance; } }

    static public RaceMgr Race {get {return instance.GetRaceMgr; } }

    RaceMgr raceMgr;
    public RaceMgr GetRaceMgr {get {return raceMgr; } }

    public delegate void GameEventDelegate();
    public event GameEventDelegate OnStartRace;
    public event GameEventDelegate OnResetRace;

    [SerializeField]
    Button startBt;
    [SerializeField]
    Button resetBt;

    void Awake()
    {
        instance = this;
        raceMgr = GetComponentInChildren<RaceMgr>();

        startBt.gameObject.SetActive(true);
        resetBt.gameObject.SetActive(false);
    }

    void Start ()
    {
        // register events

        raceMgr.OnRaceFinished += () =>
        {
            resetBt.gameObject.SetActive(true);
        };

        startBt.onClick.AddListener(
        () =>
        {
            OnStartRace();
            startBt.gameObject.SetActive(false);
        }
        );

        resetBt.onClick.AddListener(
        () =>
        {
            OnResetRace();
            resetBt.gameObject.SetActive(false);
        }
        );
    }
}


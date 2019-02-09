
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaceMgr : MonoBehaviour {

	[SerializeField]
	Text RaceMsgText = null;
	[SerializeField]
	Text LapCountText = null;
	[SerializeField]
	Text TimerText = null;
	[SerializeField, Range(1, 3)]
	int NbLaps = 3;
	int crtLap = 1;

	[SerializeField]
	bool UseCountdown = true;

	enum ERaceState {
		WAITING,
		STARTED,
		FINISHED
	}

	ERaceState raceState = ERaceState.WAITING;
	public bool HasStarted { get { return raceState == ERaceState.STARTED; } }

	float startTime = 0f;
	float raceTime = 0f;
	public float RaceTime { get { return raceTime; } }

	public delegate void RaceEventDelegate();
	public event RaceEventDelegate OnRaceInitialized;
	public event RaceEventDelegate OnRaceFinished;

	void Start() {
		GameMgr.Instance.OnStartRace += StartRaceSequence;
		GameMgr.Instance.OnResetRace += RestartRace;
		LapCountText.enabled = false;
		TimerText.enabled = false;
	}

	private void Update() {
		UpdateTimerUI();
	}

	void InitRace() {
		RaceMsgText.enabled = false;
		LapCountText.enabled = true;
		TimerText.enabled = true;
		UpdateLapUI();

		OnRaceInitialized();
	}

	void StartRaceSequence() {
		InitRace();

		if (UseCountdown)
			StartCoroutine(StartRace_Coroutine());
		else
			StartRace();
	}

	IEnumerator StartRace_Coroutine() {
		RaceMsgText.enabled = true;
		RaceMsgText.text = "3";
		yield return new WaitForSeconds(1f);
		RaceMsgText.text = "2";
		yield return new WaitForSeconds(1f);
		RaceMsgText.text = "1";
		yield return new WaitForSeconds(1f);
		RaceMsgText.text = "GO !!";
		StartRace();
		yield return new WaitForSeconds(1f);
		RaceMsgText.enabled = false;
	}

	void StartRace() {
		raceState = ERaceState.STARTED;
		startTime = Time.time;
	}

	void RestartRace() {

		StartRaceSequence();
	}

	void UpdateLapUI() {
		LapCountText.text = "Lap " + crtLap.ToString() + " / " + NbLaps.ToString();
	}

	void UpdateTimerUI() {
		if (TimerText.enabled == false || raceState != ERaceState.STARTED)
			return;

		float crtTime = Time.time - startTime;
		int min, sec, tsec;
		min = Mathf.FloorToInt(crtTime / 60.0f);
		crtTime = crtTime % 60.0f;
		sec = Mathf.FloorToInt(crtTime);
		crtTime = crtTime - sec;
		tsec = Mathf.FloorToInt(crtTime * 100.0f);
		TimerText.text = string.Format("{0:D2} : {1:D2}  : {2:D2}", min, sec, tsec);
	}
}

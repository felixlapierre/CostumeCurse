﻿using Assets.Scripts.Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class ThunderStorm : Ability
{
    private enum Phase { Cloud, Strike, Inactive }

    [SerializeField]
    private GameObject thunder;
    private Timer timer;

    private readonly int TotalThunderStrikes = 3;
    private int CurrentThunderStrike = 0;

    private readonly float timeWindowForStrikes = 3.0f;
    private readonly float goodStrikeTimeWindow = 1.5f;
    private readonly float perfectStrikeTimeWindow = 0.5f;
    private readonly float strikeTimeInterval = 2.0f;

    private int Presses;
    private readonly float ThunderCloudDuration = 5.0f;
    private readonly int ThunderCloudMinimumDamage = 10;
    private readonly int ThunderCloudMaximumDamage = 50;
    private readonly int ThunderCloudDifficultyCurve = 5;
    private readonly int ThunderStrikePerfectDamageBonus = 50;
    private readonly int ThunderStrikeGoodDamageBonus = 25;
    private int CurrentDamage;

    private Phase phase = Phase.Inactive;
    
    private readonly float ThunderCloudGrowthSpeed = 0.05f;
    private readonly float ThunderStrikeGrowthSpeed = 1.0f;

    public new void Start()
    {
        base.Start();
        timer = GetComponent<Timer>();
        thunder.SetActive(false);

        TargetSchema = new TargetSchema(
            0,
            CombatantType.Enemy,
            SelectorType.All);
    }

    new public void StartAbility()
    {
        base.StartAbility();

        CurrentDamage = 0;
        Presses = 0;
    }

    private void Update()
    {
        if (phase == Phase.Cloud)
            ThunderCloudUpdate();

        if (phase == Phase.Strike)
            ThunderstrikeUpdate();
    }

    private void ThunderstrikeUpdate()
    {
        if (timer.IsInProgress())
        {
            float progress = timer.GetProgress();

            AnimateThunderstrike(progress);

            if (Input.GetButtonDown("Action Command"))
            {
                Debug.Log("Action Command pressed during Thunderstrike Phase");

                timer.StopTimer();
            }
        }

        if (timer.IsFinished())
        {
            EvaluateThunderStrikeInput(timer.GetProgress());
            phase = Phase.Inactive;
            thunder.SetActive(false);

            if (CurrentThunderStrike < TotalThunderStrikes)
                Invoke(nameof(NewThunderStrike), UnityEngine.Random.Range(strikeTimeInterval, 1.5f * strikeTimeInterval));
            else
                EndAbility();
        }
    }

    private void AnimateThunderstrike(float progress)
    {        
        if (progress <= timeWindowForStrikes / 2.0f)
            thunder.transform.localScale += Vector3.one * ThunderStrikeGrowthSpeed * Time.deltaTime;
        else
            thunder.transform.localScale -= Vector3.one * ThunderStrikeGrowthSpeed * Time.deltaTime;

        if (WithinPerfectStrikeWindow(progress))
            thunder.GetComponent<Renderer>().material.color = Color.red;
        else if (WithinGoodStrikeWindow(progress))
            thunder.GetComponent<Renderer>().material.color = Color.blue;
        else
            thunder.GetComponent<Renderer>().material.color = Color.white;
    }

    protected override void EndAbility()
    {
        Debug.Log($"Thunderstorm Damage total: {CurrentDamage}");
        thunder.SetActive(false);

        // Deal damage to defender, wait
        // for now, let's just pick 3 random ones
        // this might be done before EndAbility
        Attack attack = new Attack(CurrentDamage);
        for(int i = 0; i < TotalThunderStrikes; i++)
            TargetedCombatants[Random.Range(0, TargetedCombatants.Length)].GetComponent<Combatant>()
                .Defend(attack);

        CombatSystem.EndTurn(this.GetComponentInParent<Combatant>().gameObject);
    }

    private void ThunderCloudUpdate()
    {
        if (timer.IsInProgress())
        {
            ThunderCloudMash();
        }
        else
        {
            EndThunderCloudPhase();
        }
    }

    private void EndThunderCloudPhase()
    {
        Debug.Log($"Thundercloud Complete with presses: {Presses}");

        CurrentDamage = (int) CalculateThunderCloudDamage();
        thunder.SetActive(false);
        Debug.Log($"Thunder Cloud Build Up damage: {CurrentDamage}");

        StartThunderStrikePhase();
    }

    private void ThunderCloudMash()
    {
        if (Input.GetButtonDown("Action Command"))
        {
            Presses++;
            thunder.transform.localScale += Vector3.one * ThunderCloudGrowthSpeed;
        }
    }

    protected override void ContinueAbilityAfterTargeting()
    {
        StartThunderCloudPhase();
    }

    private void StartThunderCloudPhase()
    {
        Presses = 0;
        phase = Phase.Cloud;

        thunder.SetActive(true);
        thunder.transform.position = transform.position + 5f * Vector3.up;
        thunder.transform.localScale = Vector3.one;
        thunder.GetComponent<Renderer>().material.color = Color.white;

        timer.StartTimer(ThunderCloudDuration);
    }

    private float CalculateThunderCloudDamage()
    {
        // Using variables from https://www.desmos.com/calculator/km7jlgm5ws
        float M = ThunderCloudMaximumDamage;
        float m = ThunderCloudMinimumDamage;
        float d = ThunderCloudDifficultyCurve;
        float p = Presses;

        // Please refer to https://www.desmos.com/calculator/km7jlgm5ws for curve
        float thunderCloudDamage = (M - m) / (Mathf.PI / 2) * Mathf.Atan(p / d) + m;

        return thunderCloudDamage;
    }


    private void StartThunderStrikePhase()
    {
        phase = Phase.Inactive;

        thunder.SetActive(true);
        thunder.transform.localScale = Vector3.one;
        thunder.transform.position = transform.position + 5f * Vector3.up + 5f * Vector3.right;

        CurrentThunderStrike = 0;

        Invoke(nameof(NewThunderStrike), UnityEngine.Random.Range(strikeTimeInterval, 1.5f * strikeTimeInterval));

    }

    private void NewThunderStrike()
    {
        CurrentThunderStrike++;
        Debug.Log($"New thunderstrike, currently on # {CurrentThunderStrike}");

        phase = Phase.Strike;

        thunder.SetActive(true);
        thunder.transform.localScale = Vector3.one;
        timer.StartTimer(timeWindowForStrikes);
    }

    // TODO: Implement user feedback
    private void EvaluateThunderStrikeInput(float timerValue)
    {
        Debug.Log("Timer Value: " + timerValue);

        if (WithinPerfectStrikeWindow(timerValue))
        {
            Debug.Log("Perfect Strike");
            CurrentDamage += ThunderStrikePerfectDamageBonus;
        }
        else if (WithinGoodStrikeWindow(timerValue))
        {
            Debug.Log("Good Strike");
            CurrentDamage += ThunderStrikeGoodDamageBonus;
        }
        else
        {
            Debug.Log("Missed Strike");
        }
    }

    private bool WithinGoodStrikeWindow(float timerValue)
    {
        return timerValue >= ((timeWindowForStrikes - goodStrikeTimeWindow) / 2.0f) &&
                    timerValue <= ((timeWindowForStrikes + goodStrikeTimeWindow) / 2.0f);
    }

    private bool WithinPerfectStrikeWindow(float timerValue)
    {
        return timerValue >= ((timeWindowForStrikes - perfectStrikeTimeWindow) / 2.0f) &&
                    timerValue <= ((timeWindowForStrikes + perfectStrikeTimeWindow) / 2.0f);
    }
}

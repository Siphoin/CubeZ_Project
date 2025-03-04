﻿using UnityEngine;

[System.Serializable]
public class SettingsZombieData
{
    [Header("Максимальная дальность видимости зомби")]
    public float maxDistanceVisible = 10;
    [Header("Максимальная скорость зомби")]
    public float maxSpeed = 3;
    [Header("Максимальное значение здоровья зомби")]
    public int maxHealth = 100;
    [Header("Максимальный урон зомби")]
    public int maxDamage = 20;
    [Header("Максимальное число зомби в ново-созданной орде")]
    public int maxZombiesCountInHorde = 4;

    [Header("Минимальное время спавна зомби")]
    public float minTimeSpawnZombie = 10f;
    [Header("Максимальное время спавна зомби")]
    public float maxTimeSpawnZombie = 15f;
    [Header("Коэфицент численности зомби с каждым днем")]
    public int zombieIncrementEveryDay = 2;

    [Header("Коэфицент численности зомби ночью")]
    public int zombieIncrementNight = 3;

    [Header("Время исчезновение трупа зомби")]
    public float timeRemove = 14;

    [Header("Время изчезновения зомби если он не виден основной камерой")]
    public float timeRemoveonGCZombie = 180.0f;


    [Header("Максиальное кол-во зомби в мире")]
    public int countZombiesWorld = 100;

    [Header("Увеличение статов зомби ночью")]
    public int incrementPowerZombieOnlyNight = 2;

    [Header("Минимальная дистанция территории спавна от игроков")]
    public int minOffsetDistanceRandomPlane = 30;

    [Header("Задержка перед оживлением зараженного трупа (в секундах)")]
    public int rebelInfectedCorpseTimeOut = 10;

    [Header("Минимальное время спавна зараженного трупа")]
    public float minInfeectedCorpsesSpawn = 15;

    [Header("Максимальное время спавна зараженного трупа")]
    public float maxInfeectedCorpsesSpawn = 30;
    public SettingsZombieData()
    {

    }

    public SettingsZombieData(SettingsZombieData copyClass)
    {
        copyClass.CopyAll(this);
    }
}
﻿using UnityEngine;
[CreateAssetMenu(menuName = "Inventory/Item/Food", order = 0)]
public class FoodItem : BaseItem
{
    [Header("Данные о еде")]
    public FoodParams dataFood = new FoodParams();



    private void Awake()
    {
        data.typeItem = TypeItem.Food;
    }

}
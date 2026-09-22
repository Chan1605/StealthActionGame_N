using System.Collections.Generic;
using UnityEngine;

public class BottleBoxRefill : MonoBehaviour
{
    [SerializeField] private GameObject[] bottlePrefabs;
    [SerializeField] private float refillDelay = 5f;

    private class SpawnPoint
    {
        public GameObject prefab;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private readonly List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
    private Transform _root;

    private bool _isWaitingRefill;
    private float _emptyTimer;

    private void Awake()
    {
        _root = transform;

        foreach (Transform child in _root)
        {
            GameObject matchedPrefab = FindMatchingPrefab(child);
            if (matchedPrefab == null) continue;

            _spawnPoints.Add(new SpawnPoint
            {
                prefab = matchedPrefab,
                localPosition = child.localPosition,
                localRotation = child.localRotation
            });
        }
    }

    private GameObject FindMatchingPrefab(Transform child)
    {
        foreach (GameObject prefab in bottlePrefabs)
        {
            if (prefab != null && child.name.StartsWith(prefab.name))
            {
                return prefab;
            }
        }
        return null;
    }

    private int CountBottles()
    {
        int count = 0;
        foreach (Transform child in _root)
        {
            if (FindMatchingPrefab(child) != null) count++;
        }
        return count;
    }

    private void Update()
    {
        if (_root == null) return;

        if (_isWaitingRefill)
        {
            _emptyTimer += Time.deltaTime;
            if (_emptyTimer >= refillDelay)
            {
                Refill();
                _isWaitingRefill = false;
            }
            return;
        }

        if (CountBottles() == 0)
        {
            _isWaitingRefill = true;
            _emptyTimer = 0f;
        }
    }

    private void Refill()
    {
        if (_root == null) return;

        foreach (SpawnPoint point in _spawnPoints)
        {
            GameObject bottle = Instantiate(point.prefab, _root);
            bottle.transform.localPosition = point.localPosition;
            bottle.transform.localRotation = point.localRotation;
        }
    }
}
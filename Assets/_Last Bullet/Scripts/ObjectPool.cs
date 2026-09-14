using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : Component
{
    T _sample;
    Transform _root;
    List<T> _poolElements = new List<T>();

    public ObjectPool(T element)
    {
        _sample = element;
    }

    public ObjectPool(T element, Transform root)
    {
        _sample = element;
        _root = root;
    }

    public T Get()
    {
        return GetElement(true);
    }

    public T GetInactive()
    {
        return GetElement(false);
    }

    private T GetElement(bool activate)
    {
        while (_poolElements.Count > 0)
        {
            T element = _poolElements[0];
            _poolElements.RemoveAt(0);
            if (element == null) continue;
            // Reparent here (safe context). Never reparent inside
            // OnEnable/OnDisable: Unity forbids SetParent during activation.
            if (_root != null)
            {
                element.transform.SetParent(_root, false);
            }
            element.gameObject.SetActive(activate);
            return element;
        }

        if (_sample == null)
        {
            Debug.LogError("[ObjectPool] Sample is missing or destroyed. Cannot create new elements.");
            return default;
        }

        Transform parent = _root != null ? _root : _sample.transform.parent;
        T newElement = Object.Instantiate(_sample, parent);
        newElement.gameObject.SetActive(activate);
        return newElement;
    }

    public void Store(T element)
    {
        Store(element, true);
    }

    public void Store(T element, bool deactivate)
    {
        if (element == null) return;
        if (_poolElements.Contains(element)) return;
        if (deactivate)
        {
            element.gameObject.SetActive(false);
        }
        _poolElements.Add(element);
    }
}


public class GameObjectPool
{
    GameObject _sample;
    List<GameObject> _poolElements = new List<GameObject>();

    public GameObjectPool(GameObject element)
    {
        _sample = element;
    }

    public GameObject Get()
    {
        if (_poolElements.Count == 0)
        {
            var newElement = Object.Instantiate(_sample, _sample.transform.parent);
            newElement.SetActive(true);
            return newElement;
        }

        var element = _poolElements[0];
        element.SetActive(true);
        _poolElements.RemoveAt(0);
        return element;
    }

    public void Store(GameObject element)
    {
        element.SetActive(false);
        _poolElements.Add(element);
    }
}

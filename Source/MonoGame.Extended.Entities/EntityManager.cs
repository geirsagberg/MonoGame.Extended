﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Entities.Systems;

namespace MonoGame.Extended.Entities
{
    public class EntityManager : UpdateSystem
    {
        private const int DefaultBagSize = 128;

        public EntityManager(ComponentManager componentManager)
        {
            _componentManager = componentManager;
            _componentManager.ComponentsChanged += OnComponentsChanged;

            _entityPool = new Pool<Entity>(() => new Entity(++_nextId, this, _componentManager), DefaultBagSize);
        }

        private readonly ComponentManager _componentManager;
        private int _nextId;

        public int Capacity => _entityBag.Capacity;
        public IEnumerable<int> Entities => _entityBag.Where(e => e != null).Select(e => e.Id);
        public int ActiveCount { get; private set; }

        private readonly Bag<Entity> _entityBag = new Bag<Entity>(DefaultBagSize);
        private readonly Pool<Entity> _entityPool;
        private readonly HashSet<int> _addedEntities = new HashSet<int>();
        private readonly HashSet<int> _removedEntities = new HashSet<int>();
        private readonly HashSet<int> _changedEntities = new HashSet<int>();
        private readonly Bag<BitVector32> _entityToComponentBits = new Bag<BitVector32>(DefaultBagSize);

        public event Action<int> EntityAdded;
        public event Action<int> EntityRemoved;
        public event Action<int> EntityChanged;

        public Entity Create()
        {
            var entity = _entityPool.Obtain();
            var id = entity.Id;
            Debug.Assert(_entityBag[id] == null);
            _entityBag[id] = entity;
            _addedEntities.Add(id);
            _entityToComponentBits[id] = new BitVector32(0);
            return entity;
        }

        public void Destroy(int entityId)
        {
            if (!_removedEntities.Contains(entityId))
                _removedEntities.Add(entityId);
        }

        public void Destroy(Entity entity)
        {
            Destroy(entity.Id);
        }

        public Entity Get(int entityId)
        {
            return _entityBag[entityId];
        }

        public BitVector32 GetComponentBits(int entityId)
        {
            return _entityToComponentBits[entityId];
        }

        private void OnComponentsChanged(int entityId)
        {
            _changedEntities.Add(entityId);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var entityId in _addedEntities)
            {
                _entityToComponentBits[entityId] = _componentManager.CreateComponentBits(entityId);
                ActiveCount++;
                EntityAdded?.Invoke(entityId);
            }

            foreach (var entityId in _changedEntities)
            {
                _entityToComponentBits[entityId] = _componentManager.CreateComponentBits(entityId);
                EntityChanged?.Invoke(entityId);
            }

            foreach (var entityId in _removedEntities)
            {
                // we must notify subscribers before removing it from the pool
                // otherwise an entity system could still be using the entity when the same id is obtained.
                EntityRemoved?.Invoke(entityId);

                var entity = _entityBag[entityId];
                if (entity != null) {
                    _entityBag[entityId] = null;
                    _componentManager.Destroy(entityId);
                    _entityToComponentBits[entityId] = default(BitVector32);
                    ActiveCount--;
                    _entityPool.Free(entity);
                }
            }

            _addedEntities.Clear();
            _removedEntities.Clear();
            _changedEntities.Clear();
        }
    }
}

using Microsoft.Xna.Framework;
using MonoGame.Extended.Collections;
using MonoGame.Extended.Entities.Systems;

namespace MonoGame.Extended.Entities
{
    public class World : SimpleDrawableGameComponent
    {
        protected readonly Bag<IUpdateSystem> UpdateSystems;
        protected readonly Bag<IDrawSystem> DrawSystems;

        protected internal World()
        {
            UpdateSystems = new Bag<IUpdateSystem>();
            DrawSystems = new Bag<IDrawSystem>();

            RegisterSystem(ComponentManager = new ComponentManager());
            RegisterSystem(EntityManager = new EntityManager(ComponentManager));
        }

        public override void Dispose()
        {
            foreach (var updateSystem in UpdateSystems)
                updateSystem.Dispose();

            foreach (var drawSystem in DrawSystems)
                drawSystem.Dispose();

            UpdateSystems.Clear();
            DrawSystems.Clear();

            base.Dispose();
        }

        internal EntityManager EntityManager { get; }
        internal ComponentManager ComponentManager { get; }

        public int EntityCount => EntityManager.ActiveCount;

        public void RegisterSystem(ISystem system)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (system is IUpdateSystem updateSystem)
                UpdateSystems.Add(updateSystem);

            if (system is IDrawSystem drawSystem)
                DrawSystems.Add(drawSystem);

            system.Initialize(this);
        }

        public Entity GetEntity(int entityId)
        {
            return EntityManager.Get(entityId);
        }

        public Entity CreateEntity()
        {
            return EntityManager.Create();
        }

        public void DestroyEntity(int entityId)
        {
            EntityManager.Destroy(entityId);
        }

        public void DestroyEntity(Entity entity)
        {
            EntityManager.Destroy(entity);
        }

        public override void Update(GameTime gameTime)
        {
            foreach (var system in UpdateSystems)
                system.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            foreach (var system in DrawSystems)
                system.Draw(gameTime);
        }
    }
}

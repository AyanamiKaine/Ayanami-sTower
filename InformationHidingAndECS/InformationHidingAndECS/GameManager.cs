namespace InformationHidingAndECS;

public class GameManager
{
    private readonly List<BaseEntity> _baseEntities = [];
    public void ProcessTicks()
    {
        foreach (var entity in _baseEntities)
        {
            entity.Tick();
        }
    }

    public void AddEntity(BaseEntity entity)
    {
        _baseEntities.Add(entity);
    }
}
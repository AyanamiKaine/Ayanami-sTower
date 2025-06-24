// Represents a one-to-many relationship where one entity can orbit another but many entities can orbit the same entity
export const connectedTo = (entity, connectedToEntity) => {
    entity.connections = new Set();

    if (!connectedToEntity.has(connectedTo)) {
        connectedToEntity.with(connectedTo, entity);
    }
    else {
        connectedToEntity.connections.add(entity);
    }

    entity.connections.add(connectedToEntity)

    return {
        isConnectedTo: (connectedToEntity) => {
            return entity.connections.has(connectedToEntity);
        },
        connectTo: (connectedToEntity) => {
            entity.connections.add(connectedToEntity);
        }
    };

};

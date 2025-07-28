/**
 * Interface for component storage implementations
 */
interface IComponentStorage {
    function setAsObject(entityId:Int, componentData:Dynamic):Void;
    function getAsObject(entityId:Int):Dynamic;
    function getPackedEntities():Array<Int>;
    function remove(entityId:Int):Void;
    function has(entityId:Int):Bool;
    
    var count(get, never):Int;
    var capacity(get, never):Int;
    var universeSize(get, never):Int;
}
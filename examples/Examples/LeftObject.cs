namespace Examples;

public record LeftObject(Guid Id, int RightObjectId);
public record RightObject(int Id, string Name);
public record ResultObject(Guid Id, RightObject? Right);
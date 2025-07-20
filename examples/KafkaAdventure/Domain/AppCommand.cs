namespace KafkaAdventure.Domain;

public record AppCommand(Commands Command, string[] Args);
public record AppResponse(Commands Command, string Reponse);
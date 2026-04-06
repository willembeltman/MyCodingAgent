using MyCodingAgent.Enums;
using MyCodingAgent.Models;

namespace MyCodingAgent.Interfaces;

public interface IEmailableAgent : IAgent
{
    Actor[] AcceptsFrom_AgentName { get; }

    void SetCurrentMessage(WorkspaceInboxMessage message);
}

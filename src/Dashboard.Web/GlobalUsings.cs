global using Dashboard.Domain.Common;
global using Dashboard.Domain.Crypto;
global using Dashboard.Domain.Entities;
global using Dashboard.Domain.Enums;
global using Dashboard.Domain.Football;
global using Dashboard.Domain.Habits;
global using Dashboard.Domain.Hvv;
global using Dashboard.Domain.Quotes;
global using Dashboard.Domain.Running;
global using Dashboard.Domain.Status;
global using Dashboard.Domain.Time;
global using Dashboard.Domain.Weather;
global using Dashboard.Domain.Whoop;

// Bewusst KEINE Dashboard.Infrastructure.*-Usings: Komponenten/Seiten arbeiten nur gegen
// Domain-Abstraktionen; Infrastructure wird ausschließlich im Composition Root (Program.cs)
// referenziert.

namespace AutoPvpSeriesGrind.Core.Localization;

internal static class L
{
    internal static class Common
    {
        public static readonly LocString Close = new("common.close", "Close");
        public static readonly LocString Cancel = new("common.cancel", "Cancel");
        public static readonly LocString Start = new("common.start", "Start");
        public static readonly LocString Stop = new("common.stop", "Stop");
        public static readonly LocString StopRun = new("common.stopRun", "Stop the run");
        public static readonly LocString FinishThenStop = new("common.finishThenStop", "Finish, then stop");
        public static readonly LocString Done = new("common.done", "Done");
        public static readonly LocString Working = new("common.working", "Working");
        public static readonly LocString Search = new("common.search", "Search");
        public static readonly LocString NoMatches = new("common.noMatches", "Nothing matches \"{0}\"");
        public static readonly LocString DragAdjustHint = new("common.dragAdjustHint", "Drag to adjust, or click the buttons.");
    }

    internal static class Shell
    {
        public static readonly LocString NavGrind = new("shell.nav.grind", "Grind");
        public static readonly LocString NavSettings = new("shell.nav.settings", "Settings");
        public static readonly LocString NavHistory = new("shell.nav.history", "History");
        public static readonly LocString NavPlugins = new("shell.nav.plugins", "Plugins");
        public static readonly LocString NavLog = new("shell.nav.log", "Console");
        public static readonly LocString NavChangelog = new("shell.nav.changelog", "Changelog");
        public static readonly LocString NavAbout = new("shell.nav.about", "About");
        public static readonly LocString Minimize = new("shell.minimize", "Minimize to the header bar");
        public static readonly LocString Restore = new("shell.restore", "Restore");
        public static readonly LocString ModeSummary = new("shell.modeSummary", "{0}, {1}");
        public static readonly LocString ModeSummaryDot = new("shell.modeSummaryDot", "{0}  ·  {1}");
        public static readonly LocString StatusLine = new("shell.statusLine", "{0}  ·  {1}");
        public static readonly LocString SessionSummary = new("shell.sessionSummary", "{0} matches  ·  {1}");
        public static readonly LocString InstallRequired = new("shell.installRequired", "Install the required plugins");
        public static readonly LocString MatchTimeLeft = new("shell.matchTimeLeft", "{0} left  ·  {1}");
        public static readonly LocString TheArena = new("shell.theArena", "the arena");
        public static readonly LocString FinishInMatch = new("shell.finish.inMatch", "this match plays out first");
        public static readonly LocString FinishQueued = new("shell.finish.queued", "the queued match plays out first");
        public static readonly LocString FinishIdle = new("shell.finish.idle", "nothing in progress, ends right away");
        public static readonly LocString FinishArmed = new("shell.finish.armed", "Stopping after this match");
        public static readonly LocString FinishKeepGoing = new("shell.finish.keepGoing", "click to keep going");
    }

    internal static class Grind
    {
        public static readonly LocString GreetingMorning = new("grind.greeting.morning", "Good morning");
        public static readonly LocString GreetingAfternoon = new("grind.greeting.afternoon", "Good afternoon");
        public static readonly LocString GreetingEvening = new("grind.greeting.evening", "Good evening");
        public static readonly LocString GreetingNight = new("grind.greeting.night", "Late night session");
        public static readonly LocString OpenPlugins = new("grind.openPlugins", "Open plugins");
        public static readonly LocString NoRunsYet = new("grind.noRunsYet", "No runs yet");
        public static readonly LocString NoRunsYetDetail = new("grind.noRunsYetDetail", "Your stats will show up here");
        public static readonly LocString LastRunExp = new("grind.lastRun.exp", "+{0} Series");
        public static readonly LocString LastRunNoExp = new("grind.lastRun.noExp", "no Series EXP");
        public static readonly LocString LastRunTitle = new("grind.lastRun.title", "Last run: {0} matches");
        public static readonly LocString LastRunDetail = new("grind.lastRun.detail", "{0}  ·  {1}");

        public static readonly LocString TitleWrappingUp = new("grind.title.wrappingUp", "Wrapping up");
        public static readonly LocString DetailWrappingUp = new("grind.detail.wrappingUp", "Finishing the last steps of the run.");
        public static readonly LocString TitleGrinding = new("grind.title.grinding", "Grinding");
        public static readonly LocString TitleAlmostThere = new("grind.title.almostThere", "Almost there");
        public static readonly LocString DetailAlmostThere = new("grind.detail.almostThere", "Install the required plugins and the bot is good to go.");
        public static readonly LocString TitleReady = new("grind.title.ready", "Ready when you are");
        public static readonly LocString DetailReady = new("grind.detail.ready", "Press Start and the queue takes it from there.");

        public static readonly LocString StageStarting = new("grind.stage.starting", "Starting");
        public static readonly LocString StageInQueue = new("grind.stage.inQueue", "In queue");
        public static readonly LocString StagePortraits = new("grind.stage.portraits", "Portraits");
        public static readonly LocString StageFighting = new("grind.stage.fighting", "Fighting");
        public static readonly LocString StageDone = new("grind.stage.done", "Done");
        public static readonly LocString StageInMatch = new("grind.stage.inMatch", "In match");

        public static readonly LocString StageDetailStarting = new("grind.stageDetail.starting", "Getting ready to queue.");
        public static readonly LocString StageDetailInQueue = new("grind.stageDetail.inQueue", "Waiting for a Casual Match.");
        public static readonly LocString StageDetailInQueueFrontline = new("grind.stageDetail.inQueueFrontline", "Waiting for a Frontline match.");
        public static readonly LocString StageDetailPortraits = new("grind.stageDetail.portraits", "The match is about to start.");
        public static readonly LocString StageDetailFighting = new("grind.stageDetail.fighting", "Fighting for the crystal.");
        public static readonly LocString StageDetailDone = new("grind.stageDetail.done", "Finishing the last steps of the run.");
        public static readonly LocString StageDetailInMatch = new("grind.stageDetail.inMatch", "Playing out the match.");

        public static readonly LocString StatusRunning = new("grind.status.running", "Running");
        public static readonly LocString StatusWrappingUp = new("grind.status.wrappingUp", "Wrapping up");
        public static readonly LocString StatusReady = new("grind.status.ready", "Ready");
        public static readonly LocString StatusSetupNeeded = new("grind.status.setupNeeded", "Setup needed");
        public static readonly LocString StatusIdle = new("grind.status.idle", "Idle");

        public static readonly LocString StopsAfterMatches = new("grind.stops.matches", "stops after {0} matches");
        public static readonly LocString StopsAtRank = new("grind.stops.rank", "stops at rank {0}");
        public static readonly LocString StopsAfterMinutes = new("grind.stops.minutes", "stops after {0} minutes");
        public static readonly LocString StopsOnCommand = new("grind.stops.command", "stops when you stop it");

        public static readonly LocString SessionComplete = new("grind.session.complete", "Session complete");
        public static readonly LocString SessionMatches = new("grind.session.matches", "{0} matches");
        public static readonly LocString SessionExp = new("grind.session.exp", "+{0} Series EXP earned");
        public static readonly LocString BackToPlan = new("grind.session.backToPlan", "Back to the plan");
    }

    internal static class Plan
    {
        public static readonly LocString Title = new("plan.title", "Plan");
        public static readonly LocString Mode = new("plan.mode", "Crystalline Conflict");
        public static readonly LocString ModeFrontline = new("plan.modeFrontline", "Frontline");
        public static readonly LocString Queue = new("plan.sentence.queue", "Queue");
        public static readonly LocString SentenceUntil = new("plan.sentence.until", "until");
        public static readonly LocString SentenceThen = new("plan.sentence.then", "then");
        public static readonly LocString SentenceEnd = new("plan.sentence.end", ".");
        public static readonly LocString UnitMatches = new("plan.unit.matches", "matches");
        public static readonly LocString UnitRank = new("plan.unit.rank", "rank");
        public static readonly LocString UnitMinutes = new("plan.unit.minutes", "minutes");
        public static readonly LocString Locked = new("plan.locked", "The plan is locked while a run is going.");

        public static readonly LocString AfterStayPhrase = new("plan.after.stay.phrase", "stay where you are");
        public static readonly LocString AfterStayLabel = new("plan.after.stay.label", "Stay where you are");
        public static readonly LocString AfterStayHelp = new("plan.after.stay.help", "Just stop. You are left standing wherever the last match dropped you.");
        public static readonly LocString AfterInnPhrase = new("plan.after.inn.phrase", "return to the inn");
        public static readonly LocString AfterInnLabel = new("plan.after.inn.label", "Return to the inn");
        public static readonly LocString AfterInnHelp = new("plan.after.inn.help", "Travel to the inn and enter your room, via Lifestream.");
        public static readonly LocString AfterLogoutPhrase = new("plan.after.logout.phrase", "log out to title");
        public static readonly LocString AfterLogoutLabel = new("plan.after.logout.label", "Log out to title");
        public static readonly LocString AfterLogoutHelp = new("plan.after.logout.help", "Log out to the title screen.");
        public static readonly LocString AfterClosePhrase = new("plan.after.close.phrase", "close the game");
        public static readonly LocString AfterCloseLabel = new("plan.after.close.label", "Close the game");
        public static readonly LocString AfterCloseHelp = new("plan.after.close.help", "Close FFXIV entirely, via XIVLauncher's /xlkill.");
        public static readonly LocString AfterGoalTitle = new("plan.after.title", "When the goal is reached");

        public static readonly LocString SeriesRank = new("plan.seriesRank", "Series rank {0}");
        public static readonly LocString RangedFaster = new("plan.rangedFaster", "Ranged jobs grind faster");
        public static readonly LocString RangedFasterHelp = new("plan.rangedFasterHelp", "Melee jobs spend more of a match closing distance, so they finish fewer matches per hour.");
        public static readonly LocString BreakEvery = new("plan.breakEvery", "Break every {0}");
        public static readonly LocString BreakEveryHelp = new("plan.breakEveryHelp", "Idles for roughly {0} minutes every {1} matches.");

        public static readonly LocString GoalMatches = new("plan.goal.matches", "{0} matches");
        public static readonly LocString GoalRank = new("plan.goal.rank", "Series rank {0}");
        public static readonly LocString GoalMinutes = new("plan.goal.minutes", "{0} minutes");
        public static readonly LocString GoalEndless = new("plan.goal.endless", "you say stop");

        public static readonly LocString TabMatches = new("plan.tab.matches", "Matches");
        public static readonly LocString TabRank = new("plan.tab.rank", "Rank");
        public static readonly LocString TabTime = new("plan.tab.time", "Time");
        public static readonly LocString TabEndless = new("plan.tab.endless", "Endless");

        public static readonly LocString WhatToQueue = new("plan.whatToQueue", "What to queue");
        public static readonly LocString QueueCasualHelp = new("plan.queue.casualHelp", "Casual matches, the fastest way to move the Series bar.");
        public static readonly LocString QueueFrontlineHelp = new("plan.queue.frontlineHelp", "The daily Frontline roulette: 72-player battles with the biggest Series EXP per match. The bot stays with its team and never runs off alone.");

        public static readonly LocString EndlessHelp = new("plan.endlessHelp", "Queues match after match until you press Stop.");
        public static readonly LocString StopAfter = new("plan.stopAfter", "Stop after");
        public static readonly LocString StopAfterMatchesHelp = new("plan.stopAfterMatchesHelp", "Counts matches that reach the results screen, so a match you abandon does not count.");
        public static readonly LocString ReachRank = new("plan.reachRank", "Reach rank");
        public static readonly LocString ReachRankHelp = new("plan.reachRankHelp", "You are rank {0} now. The run finishes the match it is in before stopping.");
        public static readonly LocString StopAfterTimeHelp = new("plan.stopAfterTimeHelp", "The timer stops queueing new matches; the one in progress still plays out.");
    }

    internal static class Run
    {
        public static readonly LocString Running = new("run.running", "Running");
        public static readonly LocString ModeCasual = new("run.modeCasual", "Crystalline Conflict, casual");
        public static readonly LocString ModeFrontline = new("run.modeFrontline", "Frontline");
        public static readonly LocString TimeLeft = new("run.timeLeft", "{0} left");
        public static readonly LocString JobPrefix = new("run.jobPrefix", "{0}  ·  ");

        public static readonly LocString HeadlinePreparing = new("run.headline.preparing", "Preparing to queue");
        public static readonly LocString HeadlineInQueue = new("run.headline.inQueue", "In queue for a casual match");
        public static readonly LocString HeadlineMatchStarting = new("run.headline.matchStarting", "Match starting  ·  {0}");
        public static readonly LocString HeadlineWrappingUp = new("run.headline.wrappingUp", "Wrapping up the session");
        public static readonly LocString HeadlineWorking = new("run.headline.working", "Working");

        public static readonly LocString GoalOf = new("run.goal.of", "/ {0}");
        public static readonly LocString GoalOfMinutes = new("run.goal.ofMinutes", "/ {0}m");
        public static readonly LocString GoalToRank = new("run.goal.toRank", "to {0}");
        public static readonly LocString GoalReached = new("run.goal.reached", "goal reached");
        public static readonly LocString GoalToGo = new("run.goal.toGo", "{0} to go");
        public static readonly LocString GoalMinutesToGo = new("run.goal.minutesToGo", "{0}m to go");
        public static readonly LocString GoalRanksToGo = new("run.goal.ranksToGo", "{0} ranks to go");
        public static readonly LocString GoalEndless = new("run.goal.endless", "runs until you stop it");
        public static readonly LocString GoalStopping = new("run.goal.stopping", "ends after this match");

        public static readonly LocString TileMatches = new("run.tile.matches", "Matches");
        public static readonly LocString TileSeriesExp = new("run.tile.seriesExp", "Series EXP");
        public static readonly LocString TileMatchesPerHour = new("run.tile.matchesPerHour", "Matches/h");
        public static readonly LocString TileElapsed = new("run.tile.elapsed", "Elapsed");
    }

    internal static class History
    {
        public static readonly LocString Title = new("history.title", "History");
        public static readonly LocString Empty = new("history.empty", "Nothing recorded yet.");
        public static readonly LocString EmptyDetail = new("history.emptyDetail", "Finish or stop a grind and it shows up here.");
        public static readonly LocString Summary = new("history.summary", "{0} runs  ·  {1} grinding  ·  {2} matches an hour");
        public static readonly LocString ChartTitle = new("history.chartTitle", "Matches per run");
        public static readonly LocString RecentRuns = new("history.recentRuns", "Recent runs");
        public static readonly LocString TileRuns = new("history.tile.runs", "Runs");
        public static readonly LocString TileMatches = new("history.tile.matches", "Matches");
        public static readonly LocString TileSeriesExp = new("history.tile.seriesExp", "Series EXP");
        public static readonly LocString TileTimeGrinding = new("history.tile.timeGrinding", "Time grinding");
        public static readonly LocString LastRuns = new("history.lastRuns", "last {0} runs");
        public static readonly LocString Best = new("history.best", "best {0}");
        public static readonly LocString PerHour = new("history.perHour", "per hour");
        public static readonly LocString UnitMatches = new("history.unit.matches", "matches");
        public static readonly LocString Series = new("history.series", "Series");
        public static readonly LocString RowSummary = new("history.rowSummary", "{0} {1} matches in {2} +{3} Series EXP");
        public static readonly LocString RowJob = new("history.rowJob", "{0}  ·  {1}");
        public static readonly LocString TooltipJob = new("history.tooltip.job", " Played as {0}");
        public static readonly LocString TooltipMatches = new("history.tooltip.matches", " {0} matches in {1}");
        public static readonly LocString TooltipExp = new("history.tooltip.exp", " +{0} Series EXP");
        public static readonly LocString Clear = new("history.clear", "Clear history");
        public static readonly LocString ClearConfirm = new("history.clearConfirm", "Delete every recorded run?");
        public static readonly LocString ClearYes = new("history.clearYes", "Yes, clear");

        public static readonly LocString JustNow = new("history.time.justNow", "just now");
        public static readonly LocString MinutesAgo = new("history.time.minutesAgo", "{0}m ago");
        public static readonly LocString HoursAgo = new("history.time.hoursAgo", "{0}h ago");
        public static readonly LocString DaysAgo = new("history.time.daysAgo", "{0}d ago");
    }

    internal static class Plugins
    {
        public static readonly LocString Title = new("plugins.title", "Plugins");
        public static readonly LocString IntroInstall = new("plugins.intro.install", "Install adds the plugin's own repository to Dalamud and queues an install. ");
        public static readonly LocString IntroFallback = new("plugins.intro.fallback", "If one-click install fails, right-click a plugin name to copy its repo URL and add it by hand under ");
        public static readonly LocString IntroPath = new("plugins.intro.path", "/xlsettings, Experimental, Custom Plugin Repositories.");
        public static readonly LocString AllInstalled = new("plugins.allInstalled", "Everything the bot needs is installed and loaded.");
        public static readonly LocPlural Missing = new("plugins.missing", "1 required plugin is missing.", "{0} required plugins are missing.");
        public static readonly LocString LinkHint = new("plugins.linkHint", "Click to open {0} Right-click to copy it.");
        public static readonly LocString Required = new("plugins.required", "Required");
        public static readonly LocString Optional = new("plugins.optional", "Optional");
        public static readonly LocString Installed = new("plugins.installed", "Installed");
        public static readonly LocString Installing = new("plugins.installing", "Installing");
        public static readonly LocString Install = new("plugins.install", "Install");

        public static readonly LocString PurposeVnavmesh = new("plugins.purpose.vnavmesh", "Pathfinding and movement to the objective during a match.");
        public static readonly LocString PurposeLifestream = new("plugins.purpose.lifestream", "Optional: only for the return-to-the-inn action when the goal is reached; travels to the inn and enters your room.");
        public static readonly LocString PurposeAutoLb = new("plugins.purpose.autoLb", "Fires your PvP Limit Break. This plugin pushes proven per-class settings to it automatically.");
    }

    internal static class Log
    {
        public static readonly LocString Title = new("log.title", "Console");
        public static readonly LocPlural Entries = new("log.entries", "{0} line in the buffer", "{0} lines in the buffer");
        public static readonly LocString Empty = new("log.empty", "Nothing logged yet. Start a run and every step the plugin takes shows up here.");
        public static readonly LocString NoMatches = new("log.noMatches", "No lines match the current filters.");
        public static readonly LocString Footer = new("log.footer", "Every line also goes to the Dalamud log (/xllog) with the {0} prefix. When reporting a bug, press Copy log and paste the result into the issue.");
        public static readonly LocString SearchHint = new("log.searchHint", "Search messages and sources");
        public static readonly LocString CopyAll = new("log.copyAll", "Copy log");
        public static readonly LocPlural CopyFiltered = new("log.copyFiltered", "Copy {0} line", "Copy {0} lines");
        public static readonly LocString CopyTooltip = new("log.copyTooltip", "Copies the lines shown below with a header naming the plugin version, Dalamud version and zone, ready to paste into a bug report.");
        public static readonly LocString Copied = new("log.copied", "Copied");
        public static readonly LocPlural CopiedLines = new("log.copiedLines", "Copied {0} line to the clipboard", "Copied {0} lines to the clipboard");
        public static readonly LocString Clear = new("log.clear", "Clear");
        public static readonly LocString ConfirmClear = new("log.confirmClear", "Click again to clear");
        public static readonly LocString Close = new("log.close", "Close");
        public static readonly LocString LevelVerbose = new("log.level.verbose", "Verbose");
        public static readonly LocString LevelDebug = new("log.level.debug", "Debug");
        public static readonly LocString LevelInfo = new("log.level.info", "Info");
        public static readonly LocString LevelWarning = new("log.level.warning", "Warnings");
        public static readonly LocString LevelError = new("log.level.error", "Errors");
        public static readonly LocString LevelTooltip = new("log.levelTooltip", "Click to show or hide these lines. Shift-click to show only this level.");
        public static readonly LocString SourceChip = new("log.sourceChip", "Source: {0}");
        public static readonly LocString SourceChipTooltip = new("log.sourceChipTooltip", "Click to stop filtering by source.");
        public static readonly LocString JumpLatest = new("log.jumpLatest", "Jump to latest");
        public static readonly LocPlural NewLines = new("log.newLines", "{0} new line", "{0} new lines");
        public static readonly LocString Showing = new("log.showing", "Showing {0} of {1}");
        public static readonly LocString ResetFilters = new("log.resetFilters", "Reset filters");
        public static readonly LocString Shortcuts = new("log.shortcuts", "Ctrl+F search · Ctrl+C copy · Shift-click selects a range · Double-click copies a line");
        public static readonly LocPlural Selected = new("log.selected", "{0} line selected", "{0} lines selected");
        public static readonly LocString CopySelection = new("log.copySelection", "Copy selection");
        public static readonly LocString ClearSelection = new("log.clearSelection", "Clear selection");
        public static readonly LocString CopyLine = new("log.copyLine", "Copy line");
        public static readonly LocString CopyToEnd = new("log.copyToEnd", "Copy from here to the end");
        public static readonly LocString OnlySource = new("log.onlySource", "Show only {0}");
        public static readonly LocString Repeated = new("log.repeated", "Repeated {0} times in a row");
        public static readonly LocString HasDetails = new("log.hasDetails", "Has a stack trace. Select the line to read it.");
    }

    internal static class Changelog
    {
        public static readonly LocString Title = new("changelog.title", "What's new");
        public static readonly LocString Subtitle = new("changelog.subtitle", "Every update, newest first.");
        public static readonly LocString Version = new("changelog.version", "Version {0}");
        public static readonly LocString Latest = new("changelog.latest", "Latest");
        public static readonly LocString New = new("changelog.new", "New");
        public static readonly LocPlural Changes = new("changelog.changes", "{0} change", "{0} changes");

        public static readonly LocString[] Release2600 =
        [
            new("changelog.r2600.1", "Added the Console page to view, filter and copy the plugin's logs"),
            new("changelog.r2600.2", "Added this changelog, with a badge whenever an update brings something new"),
            new("changelog.r2600.3", "Overhauled the About page, with the updated Discord link"),
        ];
    }

    internal static class About
    {
        public static readonly LocString SupportTitle = new("about.support.title", "Made with love and care");
        public static readonly LocString SupportBody = new("about.support.body", "This plugin is a one-person project, built in my free time because I love this game and its community. Keeping it updated takes a lot of those hours. If it has helped you, supporting me on Patreon means I can keep giving it that time. Thank you for being here.");
        public static readonly LocString SupportButton = new("about.support.button", "Support on Patreon");
        public static readonly LocString PatreonHint = new("about.support.hint", "Open Patreon, right-click to copy the link.");
        public static readonly LocString LinkHint = new("about.linkHint", "Click to open, right-click to copy the link.");
        public static readonly LocString MadeBy = new("about.madeBy", "Made by {0}");
        public static readonly LocString Version = new("about.version", "v {0}");
        public static readonly LocString Community = new("about.community", "Community");
        public static readonly LocString DiscordTitle = new("about.discordTitle", "Join the Discord");
        public static readonly LocString DiscordBody = new("about.discordBody", "Get help, report bugs, share ideas and hear about updates first.");
        public static readonly LocString GitHubTitle = new("about.githubTitle", "View on GitHub");
        public static readonly LocString GitHubBody = new("about.githubBody", "Browse the source code and every release.");
        public static readonly LocString ReminderTitle = new("about.reminder.title", "A little reminder");
        public static readonly LocString FactsTitle = new("about.facts.title", "Did you know?");
        public static readonly LocString QuotesTitle = new("about.quotes.title", "Words to live by");
        public static readonly LocString JokesTitle = new("about.jokes.title", "Just for fun");

        public static readonly LocString[] Reminders =
        [
            new("about.reminder.1", "Been at it a while? Roll your shoulders and take one slow breath."),
            new("about.reminder.2", "Hydration check. When did you last drink some water?"),
            new("about.reminder.3", "Blink a few times and let your eyes rest for a moment."),
            new("about.reminder.4", "Stand up, stretch, and shake out your hands. Future you says thanks."),
            new("about.reminder.5", "Sit up and settle in comfortably. Your back will thank you later."),
            new("about.reminder.6", "Remember to eat something today. You matter more than any score."),
            new("about.reminder.7", "Eyes feel tired? Look at something far away for twenty seconds."),
            new("about.reminder.8", "Whatever you're chasing, you're allowed to take a break whenever."),
            new("about.reminder.9", "You're doing great. Be a little kinder to yourself today."),
            new("about.reminder.10", "A glass of water and a quick stretch can reset a long session."),
            new("about.reminder.11", "Unclench your jaw and drop your shoulders. There you go."),
            new("about.reminder.12", "Rest is part of the journey too. Step away whenever you need to."),
        ];

        public static readonly LocString[] Facts =
        [
            new("about.facts.1", "Honey never spoils. Jars over 3,000 years old have been found still edible."),
            new("about.facts.2", "Octopuses have three hearts and blue blood."),
            new("about.facts.3", "A day on Venus is longer than a whole year on Venus."),
            new("about.facts.4", "Bananas are berries, but strawberries aren't."),
            new("about.facts.5", "There are more possible chess games than atoms in the observable universe."),
            new("about.facts.6", "Sharks have been around longer than trees have."),
            new("about.facts.7", "A group of flamingos is called a flamboyance."),
            new("about.facts.8", "Honeybees can recognize individual human faces."),
            new("about.facts.9", "Wombat droppings are cube shaped."),
            new("about.facts.10", "The Eiffel Tower can grow over 15 cm taller on a hot day."),
            new("about.facts.11", "Hot water can sometimes freeze faster than cold water."),
            new("about.facts.12", "A bolt of lightning is roughly five times hotter than the surface of the Sun."),
        ];

        public static readonly LocString[] Quotes =
        [
            new("about.quotes.1", "Done is better than perfect. You can always polish later."),
            new("about.quotes.2", "Small steps every day add up to surprising distances."),
            new("about.quotes.3", "Comparison is the thief of joy. Run your own race."),
            new("about.quotes.4", "Progress, not perfection."),
            new("about.quotes.5", "You don't have to be great to start, but you have to start to be great."),
            new("about.quotes.6", "Be patient with yourself. Growth takes time."),
            new("about.quotes.7", "The best time to begin was yesterday. The second best is right now."),
            new("about.quotes.8", "Celebrate the small wins. They count too."),
            new("about.quotes.9", "Slow progress is still progress."),
            new("about.quotes.10", "Your only real competition is who you were yesterday."),
        ];

        public static readonly LocString[] Jokes =
        [
            new("about.jokes.1", "Why don't scientists trust atoms? Because they make up everything."),
            new("about.jokes.2", "I would tell you a chemistry joke, but I know I wouldn't get a reaction."),
            new("about.jokes.3", "Why did the scarecrow win an award? He was outstanding in his field."),
            new("about.jokes.4", "I'm reading a book about anti-gravity. It's impossible to put down."),
            new("about.jokes.5", "Why don't skeletons fight each other? They don't have the guts."),
            new("about.jokes.6", "What do you call fake spaghetti? An impasta."),
            new("about.jokes.7", "Why did the bicycle fall over? It was two tired."),
            new("about.jokes.8", "What do you call cheese that isn't yours? Nacho cheese."),
            new("about.jokes.9", "I'm on a seafood diet. I see food, and I eat it."),
            new("about.jokes.10", "I only know 25 letters of the alphabet. I don't know y."),
        ];
    }

    internal static class Settings
    {
        public static readonly LocString Title = new("settings.title", "Settings");
        public static readonly LocString Language = new("settings.language", "Language");
        public static readonly LocString LanguageHelp = new("settings.languageHelp", "The language of this plugin's windows. Job and map names always follow the game client.");

        public static readonly LocString CatSession = new("settings.cat.session", "Session");
        public static readonly LocString CatSessionSub = new("settings.cat.sessionSub", "Pacing between matches, and the breaks a person would take.");
        public static readonly LocString CatCombat = new("settings.cat.combat", "Combat");
        public static readonly LocString CatCombatSub = new("settings.cat.combatSub", "How the bot positions itself and picks its fights.");
        public static readonly LocString CatMatch = new("settings.cat.match", "In match");
        public static readonly LocString CatMatchSub = new("settings.cat.matchSub", "The social touches the bot performs during each match.");

        public static readonly LocString SessionFootnote = new("settings.session.footnote", "How long a run lasts and what happens afterwards live on the Grind page, in the plan sentence.");
        public static readonly LocString GroupPacing = new("settings.session.pacing", "Pacing");
        public static readonly LocString LeaveDuty = new("settings.session.leaveDuty", "Leave duty after");
        public static readonly LocString LeaveDutyHelp = new("settings.session.leaveDutyHelp", "Once the results screen appears, wait this long before leaving the duty. 0 leaves as soon as the match ends.");
        public static readonly LocString Requeue = new("settings.session.requeue", "Re-queue after");
        public static readonly LocString RequeueHelp = new("settings.session.requeueHelp", "Waits a random time in this range before queueing the next match; re-queueing the instant a match ends looks robotic. 0 – 0 queues immediately.");
        public static readonly LocString GroupBreaks = new("settings.session.breaks", "Breaks");
        public static readonly LocString TakeBreaks = new("settings.session.takeBreaks", "Take breaks");
        public static readonly LocString TakeBreaksHelp = new("settings.session.takeBreaksHelp", "Idle for a while every so often, the way a person steps away between sessions.");
        public static readonly LocString BreakEvery = new("settings.session.breakEvery", "Break every");
        public static readonly LocString BreakEveryHelp = new("settings.session.breakEveryHelp", "How many matches between breaks.");
        public static readonly LocString BreakLength = new("settings.session.breakLength", "Break length");
        public static readonly LocString BreakLengthHelp = new("settings.session.breakLengthHelp", "Roughly how long each break lasts, varied by ±20% each time.");

        public static readonly LocString CombatIntroMovement = new("settings.combat.introMovement", "Behavior controls movement only: where to stand and when to back off. ");
        public static readonly LocString CombatIntroRotation = new("settings.combat.introRotation", "The built-in rotation presses your skills, Guard, Purify, Recuperate and Elixir; the required Auto PVP LB plugin fires the Limit Break, auto-configured for your class.");
        public static readonly LocString GroupCombat = new("settings.combat.group", "Combat");
        public static readonly LocString RotationPlugin = new("settings.combat.rotationPlugin", "Rotation");
        public static readonly LocString RotationPluginHelp = new("settings.combat.rotationPluginHelp", "What presses your combat skills during matches.");
        public static readonly LocString Behavior = new("settings.combat.behavior", "Behavior");
        public static readonly LocString BehaviorHelp = new("settings.combat.behaviorHelp", "How the bot positions itself and picks its fights.");
        public static readonly LocString Targeting = new("settings.combat.targeting", "Targeting");
        public static readonly LocString TargetingHelp = new("settings.combat.targetingHelp", "Who the bot attacks during a fight.");
        public static readonly LocString TargetingSmart = new("settings.targeting.smart", "Smart");
        public static readonly LocString TargetingSmartHelp = new("settings.targeting.smartHelp", "Weighs low HP, the team's focus target, squishy roles (healers first), distance and Guard, and sets the pick as your target.");
        public static readonly LocString TargetingLowestHp = new("settings.targeting.lowestHp", "Lowest HP");
        public static readonly LocString TargetingLowestHpHelp = new("settings.targeting.lowestHpHelp", "The enemy with the least HP within 25 yards, skipping anyone with Guard up while someone else is available.");
        public static readonly LocString TargetingCurrent = new("settings.targeting.current", "Your current target");
        public static readonly LocString TargetingCurrentHelp = new("settings.targeting.currentHelp", "Attacks whatever you have targeted and only picks for itself when you have nothing targeted.");
        public static readonly LocString ReactionTime = new("settings.combat.reactionTime", "Reaction time");
        public static readonly LocString ReactionTimeHelp = new("settings.combat.reactionTimeHelp", "Adds a human reaction delay before the bot changes what it's doing.");
        public static readonly LocString RecordMatches = new("settings.combat.recordMatches", "Record matches");
        public static readonly LocString RecordMatchesHelp = new("settings.combat.recordMatchesHelp", "Writes every brain decision (positions, HP, posture, reason) to a per-match log file in the plugin folder, ");
        public static readonly LocString RecordMatchesHelpSize = new("settings.combat.recordMatchesHelpSize", "for reviewing and tuning how it played. Roughly 1 MB per match; only the last 30 matches are kept.");

        public static readonly LocString RotationBuiltIn = new("settings.rotation.builtIn", "Built-in");
        public static readonly LocString RotationBuiltInHelp = new("settings.rotation.builtInHelp", "This plugin presses your job's PvP skills, Guard, Purify, Recuperate and Elixir itself. Nothing extra to install. The default.");
        public static readonly LocString RotationManual = new("settings.rotation.manual", "Manual");
        public static readonly LocString RotationManualHelp = new("settings.rotation.manualHelp", "You press your own skills. The bot still queues, moves, fires the Limit Break and re-queues.");
        public static readonly LocString GroupRotation = new("settings.rotation.group", "Rotation");
        public static readonly LocString RotationGuardHp = new("settings.rotation.guardHp", "Guard below");
        public static readonly LocString RotationGuardHpHelp = new("settings.rotation.guardHpHelp", "Press Guard when your HP falls under this. Guard blocks 90% of damage for a few seconds but ends the moment you move or act, so the bot holds still while it lasts.");
        public static readonly LocString RotationGuardOnBurst = new("settings.rotation.guardOnBurst", "Guard when bursted");
        public static readonly LocString RotationGuardOnBurstHelp = new("settings.rotation.guardOnBurstHelp", "Also Guard earlier, while your HP is dropping fast and at least two enemies are on you.");
        public static readonly LocString RotationGuardOnBurstHp = new("settings.rotation.guardOnBurstHp", "Bursted Guard below");
        public static readonly LocString RotationGuardOnBurstHpHelp = new("settings.rotation.guardOnBurstHpHelp", "The HP the burst Guard waits for.");
        public static readonly LocString RotationRecuperate = new("settings.rotation.recuperate", "Recuperate when missing");
        public static readonly LocString RotationRecuperateHelp = new("settings.rotation.recuperateHelp", "Recuperate heals a flat 15,000 for 2,500 MP; the bot presses it once you are missing at least this much.");
        public static readonly LocString RotationElixir = new("settings.rotation.elixir", "Elixir below");
        public static readonly LocString RotationElixirHelp = new("settings.rotation.elixirHelp", "Standard-issue Elixir refills HP and MP fully but takes 4.5 seconds standing still. Used when HP or MP is under this and no enemy is within 25 yards.");
        public static readonly LocString RotationAllySupport = new("settings.rotation.allySupport", "Help allies below");
        public static readonly LocString RotationAllySupportHelp = new("settings.rotation.allySupportHelp", "Heals, shields and other friendly skills go to the lowest ally under this HP; otherwise to you when you are under it.");
        public static readonly LocString RotationPurify = new("settings.rotation.purify", "Purify");
        public static readonly LocString RotationPurifyHelp = new("settings.rotation.purifyHelp", "Clear Stun, Heavy, Bind, Silence, Deep Freeze and Miracle of Nature as soon as they land.");

        public static readonly LocString StrategyRush = new("settings.strategy.rush", "Rush the crystal");
        public static readonly LocString StrategyRushHelp = new("settings.strategy.rushHelp", "No tactics: runs to the objective and stands on it. Never retreats; will feed when outnumbered.");
        public static readonly LocString StrategyDefensive = new("settings.strategy.defensive", "Defensive");
        public static readonly LocString StrategyDefensiveHelp = new("settings.strategy.defensiveHelp", "Holds the point without diving, kites when focused, retreats below ~55% HP. Ranged and healers stay far back.");
        public static readonly LocString StrategyModerate = new("settings.strategy.moderate", "Moderate");
        public static readonly LocString StrategyModerateHelp = new("settings.strategy.moderateHelp", "Balanced: short chases when ahead, falls back when outnumbered, retreats below ~35% HP. A good default.");
        public static readonly LocString StrategyAggressive = new("settings.strategy.aggressive", "Aggressive");
        public static readonly LocString StrategyAggressiveHelp = new("settings.strategy.aggressiveHelp", "Pushes the enemy line and chases kills; retreats only when nearly dead (~18% HP).");
        public static readonly LocString StrategyCustom = new("settings.strategy.custom", "Custom");
        public static readonly LocString StrategyCustomHelp = new("settings.strategy.customHelp", "Hand-tuned: every threshold below is yours to set. Starts from the Moderate baseline.");

        public static readonly LocString FrontlineBehavior = new("settings.frontline.behavior", "Frontline behavior");
        public static readonly LocString FrontlineBehaviorHelp = new("settings.frontline.behaviorHelp", "How the bot moves with its team in Frontline.");
        public static readonly LocString FrontlineDefensive = new("settings.frontline.defensive", "Defensive");
        public static readonly LocString FrontlineDefensiveHelp = new("settings.frontline.defensiveHelp", "Sticks to the back of the group, falls back below ~55% HP and keeps well clear of the enemy line.");
        public static readonly LocString FrontlineModerate = new("settings.frontline.moderate", "Moderate");
        public static readonly LocString FrontlineModerateHelp = new("settings.frontline.moderateHelp", "Holds a normal spot in the group and falls back below ~45% HP or when two enemies focus you. A good default.");
        public static readonly LocString FrontlineAggressive = new("settings.frontline.aggressive", "Aggressive");
        public static readonly LocString FrontlineAggressiveHelp = new("settings.frontline.aggressiveHelp", "Stands at the very front of the group, shrugs off focus longer and falls back only below ~30% HP.");
        public static readonly LocString FrontlineCustom = new("settings.frontline.custom", "Custom");
        public static readonly LocString FrontlineCustomHelp = new("settings.frontline.customHelp", "Hand-tuned: every Frontline distance and threshold below is yours to set. Starts from the Moderate baseline.");
        public static readonly LocString FrontlineGroupPosition = new("settings.frontlineCustom.position", "Where to stand in the group");
        public static readonly LocString FrontlineFrontOffset = new("settings.frontlineCustom.frontOffset", "Melee: stand ahead by");
        public static readonly LocString FrontlineFrontOffsetHelp = new("settings.frontlineCustom.frontOffsetHelp", "As melee or tank, how far ahead of the group's centre toward the enemy to stand.");
        public static readonly LocString FrontlineBackOffset = new("settings.frontlineCustom.backOffset", "Ranged: stand behind by");
        public static readonly LocString FrontlineBackOffsetHelp = new("settings.frontlineCustom.backOffsetHelp", "As ranged or healer, how far behind the group's centre to stand.");
        public static readonly LocString FrontlineRetreatOffset = new("settings.frontlineCustom.retreatOffset", "Fall back behind by");
        public static readonly LocString FrontlineRetreatOffsetHelp = new("settings.frontlineCustom.retreatOffsetHelp", "When hurt or focused, how far behind the group's centre to retreat.");
        public static readonly LocString FrontlineGroupCohesion = new("settings.frontlineCustom.cohesion", "Staying together");
        public static readonly LocString FrontlineRegroupDistance = new("settings.frontlineCustom.regroupDistance", "Rejoin when farther than");
        public static readonly LocString FrontlineRegroupDistanceHelp = new("settings.frontlineCustom.regroupDistanceHelp", "If you drift farther than this from the group, everything else waits until you are back with it.");
        public static readonly LocString FrontlineEngageRange = new("settings.frontlineCustom.engageRange", "Fight enemies within");
        public static readonly LocString FrontlineEngageRangeHelp = new("settings.frontlineCustom.engageRangeHelp", "Only enemies this close count as targets; nobody is chased beyond the group.");
        public static readonly LocString FrontlineHurtBelow = new("settings.frontlineCustom.hurtBelow", "Fall back below");
        public static readonly LocString FrontlineHurtBelowHelp = new("settings.frontlineCustom.hurtBelowHelp", "Under this HP you move behind the group until you recover.");
        public static readonly LocString FrontlineFocusedHurtBelow = new("settings.frontlineCustom.focusedHurtBelow", "Fall back when focused below");
        public static readonly LocString FrontlineFocusedHurtBelowHelp = new("settings.frontlineCustom.focusedHurtBelowHelp", "When enough enemies aim at you (see In danger at), fall back already at this HP.");
        public static readonly LocString FrontlineRecoverAbove = new("settings.frontlineCustom.recoverAbove", "Return above");
        public static readonly LocString FrontlineRecoverAboveHelp = new("settings.frontlineCustom.recoverAboveHelp", "Once healed above this, take your spot in the group again.");
        public static readonly LocString FrontlineGroupRiding = new("settings.frontlineCustom.riding", "Riding back");
        public static readonly LocString FrontlineMountDistance = new("settings.frontlineCustom.mountDistance", "Mount when farther than");
        public static readonly LocString FrontlineMountDistanceHelp = new("settings.frontlineCustom.mountDistanceHelp", "After a respawn or a split, mount up for the ride back when the group is at least this far away.");
        public static readonly LocString FrontlineEnemyAware = new("settings.frontlineCustom.enemyAware", "Enemy nearby within");
        public static readonly LocString FrontlineEnemyAwareHelp = new("settings.frontlineCustom.enemyAwareHelp", "An enemy this close means no mounting, and dismount at once.");

        public static readonly LocString ReactionOff = new("settings.reaction.off", "Off");
        public static readonly LocString ReactionOffHelp = new("settings.reaction.offHelp", "Reacts instantly, with frame-perfect movement.");
        public static readonly LocString ReactionLight = new("settings.reaction.light", "Light");
        public static readonly LocString ReactionLightHelp = new("settings.reaction.lightHelp", "~80–220 ms reaction delay.");
        public static readonly LocString ReactionRealistic = new("settings.reaction.realistic", "Realistic");
        public static readonly LocString ReactionRealisticHelp = new("settings.reaction.realisticHelp", "~140–380 ms reaction delay. A good default.");
        public static readonly LocString ReactionHeavy = new("settings.reaction.heavy", "Heavy");
        public static readonly LocString ReactionHeavyHelp = new("settings.reaction.heavyHelp", "~260–650 ms reaction delay, clearly unhurried.");

        public static readonly LocString GroupHealth = new("settings.custom.health", "Health");
        public static readonly LocString RetreatBelow = new("settings.custom.retreatBelow", "Retreat below");
        public static readonly LocString RetreatBelowHelp = new("settings.custom.retreatBelowHelp", "When enemies are on you, or your side is outnumbered, and your HP drops under this, fall back to safety.");
        public static readonly LocString RejoinAbove = new("settings.custom.rejoinAbove", "Rejoin above");
        public static readonly LocString RejoinAboveHelp = new("settings.custom.rejoinAboveHelp", "Once you have healed back above this much HP, return to the fight.");
        public static readonly LocString AlwaysFleeBelow = new("settings.custom.alwaysFleeBelow", "Always flee below");
        public static readonly LocString AlwaysFleeBelowHelp = new("settings.custom.alwaysFleeBelowHelp", "If your HP drops under this, run for safety no matter what else is happening.");
        public static readonly LocString HeavyDamage = new("settings.custom.heavyDamage", "Heavy-damage trigger");
        public static readonly LocString HeavyDamageHelp = new("settings.custom.heavyDamageHelp", "If your HP is dropping faster than this each second, treat it as taking heavy damage and back off early.");

        public static readonly LocString GroupAggression = new("settings.custom.aggression", "Aggression");
        public static readonly LocString ChaseAhead = new("settings.custom.chaseAhead", "Chase when ahead by");
        public static readonly LocString ChaseAheadHelp = new("settings.custom.chaseAheadHelp", "How much stronger your nearby side must be before you chase a kill. Wounded fighters count for less, and each enemy already dead counts as half a fighter. 0 = chase even fights; below 0 = chase even when slightly outnumbered.");
        public static readonly LocString FallBackBehind = new("settings.custom.fallBackBehind", "Fall back when behind by");
        public static readonly LocString FallBackBehindHelp = new("settings.custom.fallBackBehindHelp", "How much stronger the nearby enemies must be before you retreat to your team. Wounded fighters count for less. 0 = fall back the moment they outnumber you.");

        public static readonly LocString GroupPositioning = new("settings.custom.positioning", "Where to stand");
        public static readonly LocString MeleeHold = new("settings.custom.meleeHold", "Melee: hold near crystal");
        public static readonly LocString MeleeHoldHelp = new("settings.custom.meleeHoldHelp", "As melee or tank with no one to chase, how far from the crystal to stand.");
        public static readonly LocString MeleeChase = new("settings.custom.meleeChase", "Melee: chase up to");
        public static readonly LocString MeleeChaseHelp = new("settings.custom.meleeChaseHelp", "As melee or tank chasing a target, how close to get before attacking.");
        public static readonly LocString RangedHold = new("settings.custom.rangedHold", "Ranged: hold behind crystal");
        public static readonly LocString RangedHoldHelp = new("settings.custom.rangedHoldHelp", "As ranged or healer holding position, how far behind the crystal to stand.");
        public static readonly LocString RangedAttack = new("settings.custom.rangedAttack", "Ranged: attack from");
        public static readonly LocString RangedAttackHelp = new("settings.custom.rangedAttackHelp", "As ranged or healer, how far from your target to stand while attacking.");
        public static readonly LocString KeepBack = new("settings.custom.keepBack", "Keep back while waiting");
        public static readonly LocString KeepBackHelp = new("settings.custom.keepBackHelp", "While waiting for teammates to arrive, keep at least this far from the enemy group.");

        public static readonly LocString GroupFocus = new("settings.custom.focus", "When enemies focus you");
        public static readonly LocString InDangerAt = new("settings.custom.inDangerAt", "In danger at");
        public static readonly LocString InDangerAtHelp = new("settings.custom.inDangerAtHelp", "How many enemies aiming at you before you count as in danger. Works with Retreat below: the more attackers, the sooner you fall back.");
        public static readonly LocString SidestepAt = new("settings.custom.sidestepAt", "Sidestep at");
        public static readonly LocString SidestepAtHelp = new("settings.custom.sidestepAtHelp", "When at least this many enemies are aiming at you, sidestep to safety even at full HP.");
        public static readonly LocString SidestepDistance = new("settings.custom.sidestepDistance", "Sidestep distance");
        public static readonly LocString SidestepDistanceHelp = new("settings.custom.sidestepDistanceHelp", "When enemies focus you, how far to move in one sidestep, toward whatever spot is safest and closest to your team.");
        public static readonly LocString RetreatStep = new("settings.custom.retreatStep", "Retreat step");
        public static readonly LocString RetreatStepHelp = new("settings.custom.retreatStepHelp", "How far each step of a retreat takes you. The direction is chosen to slip past enemies, stay near your team, and break their line of sight.");

        public static readonly LocString GroupLimits = new("settings.custom.limits", "Distances & limits");
        public static readonly LocString BackupRange = new("settings.custom.backupRange", "Backup nearby range");
        public static readonly LocString BackupRangeHelp = new("settings.custom.backupRangeHelp", "A teammate within this distance counts as backup. With nobody inside it, you act as if you are alone.");
        public static readonly LocString EnemyNearRange = new("settings.custom.enemyNearRange", "Enemy-near range");
        public static readonly LocString EnemyNearRangeHelp = new("settings.custom.enemyNearRangeHelp", "Enemies within this distance of you count toward being outnumbered.");
        public static readonly LocString FightZone = new("settings.custom.fightZone", "Fight-zone size");
        public static readonly LocString FightZoneHelp = new("settings.custom.fightZoneHelp", "The area around the crystal used to judge who is winning the fight. When no enemy is inside it, the point counts as free and you stand on it to push.");
        public static readonly LocString MaxChase = new("settings.custom.maxChase", "Max chase from crystal");
        public static readonly LocString MaxChaseHelp = new("settings.custom.maxChaseHelp", "Never chase a target further than this from the crystal. Enemies past it are ignored.");
        public static readonly LocString MaxFromTeam = new("settings.custom.maxFromTeam", "Max distance from team");
        public static readonly LocString MaxFromTeamHelp = new("settings.custom.maxFromTeamHelp", "Never wander further than this from the middle of your team.");
        public static readonly LocString ResetDefaults = new("settings.custom.resetDefaults", "Reset to defaults");
        public static readonly LocString ResetDefaultsHelp = new("settings.custom.resetDefaultsHelp", "Sets every value above back to the Moderate baseline. Hold Ctrl and click to confirm.");

        public static readonly LocString GroupMatchIntro = new("settings.match.intro", "Match intro");
        public static readonly LocString SayHello = new("settings.match.sayHello", "Say hello");
        public static readonly LocString SayHelloHelp = new("settings.match.sayHelloHelp", "Sends /quickchat Hello once during the portrait phase, at a random moment so it doesn't look scripted.");
        public static readonly LocString Chance = new("settings.match.chance", "Chance");
        public static readonly LocString HelloChanceHelp = new("settings.match.helloChanceHelp", "How often the hello actually fires. Lower means it sometimes stays silent.");
        public static readonly LocString After = new("settings.match.after", "After");
        public static readonly LocString HelloAfterHelp = new("settings.match.helloAfterHelp", "Waits a random time in this range after the portraits appear, so the greeting never fires the instant the intro starts.");
        public static readonly LocString Emotes = new("settings.match.emotes", "Occasional emotes");
        public static readonly LocString EmotesHelp = new("settings.match.emotesHelp", "Plays a random friendly emote (wave, cheer, salute, and the like) at a random moment of the pre-match countdown, sometimes twice. Waits until your character is free so the emote actually plays.");
        public static readonly LocString GroupResults = new("settings.match.results", "Results screen");
        public static readonly LocString GoodMatch = new("settings.match.goodMatch", "Say “Good Match”");
        public static readonly LocString GoodMatchHelp = new("settings.match.goodMatchHelp", "Sends /quickchat \"Good Match\" when the results screen appears at the end of a match.");
        public static readonly LocString GoodMatchChanceHelp = new("settings.match.goodMatchChanceHelp", "How often \"Good Match\" actually fires after a match.");
        public static readonly LocString GoodMatchAfterHelp = new("settings.match.goodMatchAfterHelp", "Waits a random time in this range after the results screen appears. If it lands later than \"Leave duty after\" (under Session), the bot leaves first and skips the goodbye.");

        public static readonly LocString FormatSeconds = new("settings.format.seconds", "%d s");
        public static readonly LocString FormatMinutes = new("settings.format.minutes", "%d min");
        public static readonly LocString FormatMatches = new("settings.format.matches", "%d matches");
        public static readonly LocString FormatYards = new("settings.format.yards", "%d yd");
        public static readonly LocString FormatAttackers = new("settings.format.attackers", "%d attackers");
        public static readonly LocString FormatPercent = new("settings.format.percent", "%d%%");
        public static readonly LocString FormatCount = new("settings.format.count", "%d");
        public static readonly LocString FormatHp = new("settings.format.hp", "%d HP");
        public static readonly LocString FormatPercentOfMatches = new("settings.format.percentOfMatches", "%d%% of matches");
        public static readonly LocString FormatPercentPerSecond = new("settings.format.percentPerSecond", "%d%%/s");
    }

    internal static class Brain
    {
        public static readonly LocString LegendEnemy = new("brain.legend.enemy", "● enemy");
        public static readonly LocString LegendTarget = new("brain.legend.target", "◎ target");
        public static readonly LocString LegendAlly = new("brain.legend.ally", "● ally");
        public static readonly LocString LegendPoint = new("brain.legend.point", "◆ point");
        public static readonly LocString Title = new("brain.title", "Combat brain");
        public static readonly LocString NavFallback = new("brain.navFallback", "vnavmesh offline, chat fallback");
        public static readonly LocString Range = new("brain.range", "~{0}y");
        public static readonly LocString Sprinting = new("brain.sprinting", "sprinting");
        public static readonly LocString PostureIdle = new("brain.posture.idle", "Idle");
        public static readonly LocString PostureHold = new("brain.posture.hold", "Hold");
        public static readonly LocString PosturePush = new("brain.posture.push", "Push");
        public static readonly LocString PostureStage = new("brain.posture.stage", "Stage");
        public static readonly LocString PostureReposition = new("brain.posture.reposition", "Reposition");
        public static readonly LocString PostureRegroup = new("brain.posture.regroup", "Regroup");
        public static readonly LocString PostureRetreat = new("brain.posture.retreat", "Retreat");
        public static readonly LocString Enemy = new("brain.enemy", "Enemy");
        public static readonly LocString Ally = new("brain.ally", "Ally");
        public static readonly LocString Point = new("brain.point", "Point");
        public static readonly LocString Target = new("brain.target", "Target");
        public static readonly LocString RoleTank = new("brain.role.tank", "tank");
        public static readonly LocString RoleMelee = new("brain.role.melee", "melee");
        public static readonly LocString RoleRanged = new("brain.role.ranged", "ranged");
        public static readonly LocString RoleHealer = new("brain.role.healer", "healer");
        public static readonly LocString RoleUnknown = new("brain.role.unknown", "unknown role");
    }

    internal static class Plugin
    {
        public static readonly LocString CommandHelp = new("plugin.commandHelp", "Toggle the Auto PVP Series Grind window. /apsg config | stats | deps | log | changelog | about | target | objects.");
        public static readonly LocString CommandHelpAlias = new("plugin.commandHelpAlias", "Alias for /apsg.");
    }
}

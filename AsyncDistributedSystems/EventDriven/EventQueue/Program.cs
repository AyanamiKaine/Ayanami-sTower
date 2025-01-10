using EventQueue;

EventQueueService queueService = new();
queueService.RunAsync().Wait();
using i3GNetworkSwitcher.Core.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace i3GNetworkSwitcher.Core.Scheduler
{
    class NetworkScheduler
    {
        public delegate Task ScheduledEventExecuted(NetworkScheduler scheduler, ScheduledCommandEvent evt);
        public delegate Task ScheduledEventFailed(NetworkScheduler scheduler, ScheduledCommandEvent evt, Exception ex);

        public NetworkScheduler(NetworkController controller, string databaseFilename, TimeSpan offset)
        {
            //Set
            this.controller = controller;
            this.databaseFilename = databaseFilename;
            this.offset = offset;

            //Read database if it exists
            if (!TryReadEventsFrom(databaseFilename))
            {
                //Failed. Try reading backup file
                if (TryReadEventsFrom(databaseFilename + ".bak"))
                {
                    //Save to main file
                    Save();
                } else
                {
                    //Initialize new event list
                    events = new List<ScheduledCommandEvent>();
                }
            }
        }

        private readonly NetworkController controller;
        private readonly string databaseFilename;
        private readonly TimeSpan offset; // Offset added to time for calculation
        private readonly object mutex = new object();
        private List<ScheduledCommandEvent> events;

        /// <summary>
        /// Event raised when an event was successfully executed.
        /// </summary>
        public event ScheduledEventExecuted OnScheduledEventExecuted;

        /// <summary>
        /// Event raised when an event failed to execute.
        /// </summary>
        public event ScheduledEventFailed OnScheduledEventFailed;

        /// <summary>
        /// Gets current scheduled events.
        /// </summary>
        public IReadOnlyList<ScheduledCommandEvent> Events
        {
            get
            {
                List<ScheduledCommandEvent> result = new List<ScheduledCommandEvent>();
                lock (mutex)
                    result.AddRange(events);
                return result;
            }
        }

        /// <summary>
        /// Attempts to read events from a specified filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool TryReadEventsFrom(string filename)
        {
            //If the file doesn't exist, abort
            if (!File.Exists(filename))
                return false;

            //Attempt to read
            try
            {
                events = JsonConvert.DeserializeObject<List<ScheduledCommandEvent>>(File.ReadAllText(filename));
            } catch
            {
                return false;
            }
            return events != null;
        }

        /// <summary>
        /// Writes current events to file.
        /// </summary>
        private void Save()
        {
            lock (mutex)
            {
                //Serialize
                string ser = JsonConvert.SerializeObject(events);

                //Create backup filename
                string backupFilename = databaseFilename + ".bak";

                //Delete old backup
                if (File.Exists(backupFilename))
                    File.Delete(backupFilename);

                //Move existing to backup
                if (File.Exists(databaseFilename))
                    File.Move(databaseFilename, backupFilename);

                //Write to main
                File.WriteAllText(databaseFilename, ser);
            }
        }

        /// <summary>
        /// Runs scheduler in the background.
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //Look to see if any events have passed. If multiple, pick the latest.
                //This is so that if the application was closed and multiple had passed, the most recently active one will be picked
                DateTime time = DateTime.UtcNow;
                ScheduledCommandEvent evt = null;
                lock (mutex)
                {
                    for (int i = 0; i < events.Count; i++)
                    {
                        if ((events[i].Time + offset) < time)
                        {
                            //Set found event to this if no other found events were found, otherwise check if it is newer
                            if (evt == null || events[i].Time > evt.Time)
                                evt = events[i];

                            //Consume from event list
                            events.RemoveAt(i);
                            i--;
                        }
                    }
                }

                //If one was found, execute it
                if (evt != null)
                {
                    //Execute
                    try
                    {
                        //Execute it
                        await controller.ExecuteCommand(evt.Command);

                        //Send event
                        await OnScheduledEventExecuted?.Invoke(this, evt);
                    } catch (Exception ex)
                    {
                        await OnScheduledEventFailed?.Invoke(this, evt, ex);
                    }

                    //Save to disk
                    Save();
                }

                //Delay to try again
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Adds this event to the queue and returns it.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="description"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public ScheduledCommandEvent AddEvent(DateTime time, string description, NetworkControlCommand command)
        {
            ScheduledCommandEvent evt;
            lock (mutex)
            {
                //Generate a free GUID
                Guid id;
                do
                {
                    id = Guid.NewGuid();
                } while (events.Where(x => x.Id == id).Any());

                //Create and add event
                evt = new ScheduledCommandEvent
                {
                    Id = id,
                    Time = time,
                    Description = description,
                    Command = command
                };
                events.Add(evt);

                //Save
                Save();
            }
            return evt;
        }

        /// <summary>
        /// Updates an event in a thread-safe manner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="time"></param>
        /// <param name="description"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public ScheduledCommandEvent UpdateEvent(Guid id, DateTime time, string description, NetworkControlCommand command)
        {
            ScheduledCommandEvent evt = null;
            lock (mutex)
            {
                //Find event with this ID
                foreach (var e in events)
                {
                    if (e.Id == id)
                        evt = e;
                }

                //If not found, abort
                if (evt == null)
                    throw new Exception("Failed to find event with matching ID.");

                //Update
                evt.Time = time;
                evt.Description = description;
                evt.Command = command;

                //Save
                Save();
            }
            return evt;
        }

        /// <summary>
        /// Deletes an event in a thread-safe manner
        /// </summary>
        /// <param name="id"></param>
        /// <param name="time"></param>
        /// <param name="description"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public ScheduledCommandEvent DeleteEvent(Guid id)
        {
            ScheduledCommandEvent evt = null;
            lock (mutex)
            {
                //Find event with this ID
                foreach (var e in events)
                {
                    if (e.Id == id)
                        evt = e;
                }

                //If not found, abort
                if (evt == null)
                    throw new Exception("Failed to find event with matching ID.");

                //Delete
                if (!events.Remove(evt))
                    throw new Exception("Unknown error removing event from list.");

                //Save
                Save();
            }
            return evt;
        }
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateRoomsRevitPlugin
{
    [Transaction(TransactionMode.Manual)]
    public class RoomCreator : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document document = commandData.Application.ActiveUIDocument.Document;

            List<Level> levelsList = new FilteredElementCollector(document)
                                    .OfClass(typeof(Level))
                                    .OfType<Level>()
                                    .ToList();

            Phase constractionStage = getConstructionStage(document);

            placeRooms(document, levelsList, constractionStage);
            return Result.Succeeded;
        }
        public Phase getConstructionStage(Document document)
        {
            View activeView = document.ActiveView;
            Parameter parameter = activeView.get_Parameter(BuiltInParameter.VIEW_PHASE);
            ElementId eID = parameter.AsElementId();
            Phase constractionStage = document.GetElement(eID) as Phase;
            return constractionStage;
        }
        public void placeRooms(Document document, List<Level> levelsList, Phase constractionStage)
        {
            Transaction transaction = new Transaction(document, "Размещение помещений");
            transaction.Start();
            List<Room> rooms = insertNewRoomsInModel(document, levelsList, constractionStage);
            transaction.Commit();
        }
        public List<Room> insertNewRoomsInModel(Document document, List<Level> levelsList, Phase constructionStage)
        {
            List<Room> roomsList = new List<Room>();

            for (int serialNumberOfTheFloor = 0; serialNumberOfTheFloor < levelsList.Count(); serialNumberOfTheFloor++)
            {
                List<Room> roomsOnTheFloorList = insertNewRoomsInLevelPlane(document, levelsList, serialNumberOfTheFloor, constructionStage); ;
                roomsList.AddRange(roomsOnTheFloorList);
            }
            return roomsList;
        }
        public List<Room> insertNewRoomsInLevelPlane(Document document, List<Level> levelsList, int serialNumberOfTheFloor, Phase constructionStage)
        {
            List<Room> roomsOnTheFloorList = new List<Room>();

            PlanTopology planTopology = document.get_PlanTopology(levelsList[serialNumberOfTheFloor]);
            int serialNumberOfTheRoomOnTheFloor = 1;
            foreach (PlanCircuit circuit in planTopology.Circuits)
            {
                Room newRoom = insertNewRoom(document, circuit, constructionStage, serialNumberOfTheFloor, serialNumberOfTheRoomOnTheFloor);
                try
                {
                    newRoom = setNewRoomBuiltInParameters(document, newRoom, levelsList[serialNumberOfTheFloor + 1]);
                }
                catch
                {
                }
                roomsOnTheFloorList.Add(newRoom);
                serialNumberOfTheRoomOnTheFloor++;
            }
            return roomsOnTheFloorList;
        }
        private Room insertNewRoom(Document document, PlanCircuit circuit, Phase constructionStage, int serialNumberOfTheFloor, int serialNumberOfTheRoomOnTheFloor)
        {
            Room newScheduleRoom = createScheduleRoom(document, constructionStage, serialNumberOfTheFloor, serialNumberOfTheRoomOnTheFloor);
            Room newRoom = document.Create.NewRoom(newScheduleRoom, circuit);            
            return newRoom;
        }
        private Room createScheduleRoom(Document document, Phase constructionStage, int serialNumberOfTheFloor, int serialNumberOfTheRoomOnTheFloor)
        {
            Room room = document.Create.NewRoom(constructionStage);
            room.Name = null;
            room.Number = (serialNumberOfTheFloor + 1).ToString() + "_" + serialNumberOfTheRoomOnTheFloor.ToString();
            return room;
        }
        private Room setNewRoomBuiltInParameters(Document document, Room room, Level level)
        {
            room.get_Parameter(BuiltInParameter.ROOM_UPPER_LEVEL).Set(level.Id);
            room.get_Parameter(BuiltInParameter.ROOM_UPPER_OFFSET).Set(-getOverlapThickness(document));
            return room;
        }
        private double getOverlapThickness(Document document)
        {
            Floor overlap = new FilteredElementCollector(document)
                        .OfClass(typeof(Floor))
                        .OfType<Floor>()
                        .FirstOrDefault();
            return overlap.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble();
        }
    }
}



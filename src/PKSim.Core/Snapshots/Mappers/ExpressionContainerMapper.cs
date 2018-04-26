﻿using System.Threading.Tasks;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Services;
using OSPSuite.Utility.Extensions;
using PKSim.Assets;
using PKSim.Core.Model;
using PKSim.Core.Services;

namespace PKSim.Core.Snapshots.Mappers
{
   public class ExpressionContainerMapperContext
   {
      public IndividualMolecule Molecule { get; set; }
      public ISimulationSubject SimulationSubject { get; set; }
   }

   public class ExpressionContainerMapper : SnapshotMapperBase<MoleculeExpressionContainer, ExpressionContainer, ExpressionContainerMapperContext>
   {
      private readonly ParameterMapper _parameterMapper;
      private readonly ITransportContainerUpdater _transportContainerUpdater;
      private readonly ILogger _logger;
      private readonly MembraneLocation _defaultMembraneLocation;

      public ExpressionContainerMapper(
         ParameterMapper parameterMapper, 
         ITransportContainerUpdater transportContainerUpdater, 
         ILogger logger)
      {
         _parameterMapper = parameterMapper;
         _transportContainerUpdater = transportContainerUpdater;
         _logger = logger;
         _defaultMembraneLocation = MembraneLocation.Basolateral;
      }

      public override async Task<ExpressionContainer> MapToSnapshot(MoleculeExpressionContainer expressionContainer)
      {
         var transportedExpressionContainer = expressionContainer as TransporterExpressionContainer;
         var expressionParameter = expressionContainer.RelativeExpressionParameter;

         if (!shouldMapContainer(expressionParameter, transportedExpressionContainer))
            return null;

         var snapshot = await SnapshotFrom(expressionContainer, x => { x.Name = expressionContainer.Name; });

         if (expressionParameter.ShouldExportToSnapshot())
            await _parameterMapper.UpdateSnapshotFromParameter(snapshot, expressionParameter);

         mapTransporterExpressionProperties(snapshot, transportedExpressionContainer);

         return snapshot;
      }

      private void mapTransporterExpressionProperties(ExpressionContainer snapshot, TransporterExpressionContainer transporterExpressionContainer)
      {
         if (transporterExpressionContainer == null)
            return;

         snapshot.MembraneLocation = SnapshotValueFor(transporterExpressionContainer.MembraneLocation, _defaultMembraneLocation);
      }

      private bool shouldMapContainer(IParameter expressionParameter, TransporterExpressionContainer transportedExpressionContainer)
      {
         if (expressionParameter.ShouldExportToSnapshot())
            return true;

         if (transportedExpressionContainer == null)
            return false;

         return transportedExpressionContainer.MembraneLocation != _defaultMembraneLocation;
      }

      public override async Task<MoleculeExpressionContainer> MapToModel(ExpressionContainer snapshot, ExpressionContainerMapperContext context)
      {
         if (snapshot == null)
            return null;

         var molecule = context.Molecule;
         var individual = context.SimulationSubject;

         var expressionContainer = molecule.ExpressionContainer(snapshot.Name);
         if (expressionContainer == null)
         {
            _logger.AddWarning(PKSimConstants.Error.RelativeExpressionContainerNotFound(snapshot.Name));
            return null;
         }

         var expressionParameter = expressionContainer.RelativeExpressionParameter;
         await _parameterMapper.MapToModel(snapshot, expressionParameter);

         if (!(molecule is IndividualTransporter transporter))
            return expressionContainer;

         var species = individual.Species.Name;
         var transporterExpressionContainer = expressionContainer.DowncastTo<TransporterExpressionContainer>();
         var membraneLocation = ModelValueFor(snapshot.MembraneLocation, _defaultMembraneLocation);
         _transportContainerUpdater.UpdateTransporterFromTemplate(transporterExpressionContainer, species, membraneLocation, transporter.TransportType);

         return expressionContainer;
      }
   }
}
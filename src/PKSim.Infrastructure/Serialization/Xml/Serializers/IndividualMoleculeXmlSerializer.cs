using PKSim.Core.Model;

namespace PKSim.Infrastructure.Serialization.Xml.Serializers
{
   public abstract class IndividualMoleculeXmlSerializer<TMolecule> : PKSimContainerXmlSerializer<TMolecule> where TMolecule : IndividualMolecule
   {
      public override void PerformMapping()
      {
         base.PerformMapping();
         Map(x => x.Ontogeny);
         Map(x => x.QueryConfiguration);
      }
   }

   public abstract class IndividualProteinXmlSerializer<TProtein> : IndividualMoleculeXmlSerializer<TProtein> where TProtein : IndividualProtein
   {
      public override void PerformMapping()
      {
         base.PerformMapping();
         Map(x => x.TissueLocation);
         Map(x => x.IntracellularVascularEndoLocation);
         Map(x => x.MembraneLocation);
      }
   }

   public class IndividualEnzymeXmlSerializer : IndividualProteinXmlSerializer<IndividualEnzyme>
   {
   }

   public class IndividualTransporterXmlSerializer : IndividualMoleculeXmlSerializer<IndividualTransporter>
   {
      public override void PerformMapping()
      {
         base.PerformMapping();
         Map(x => x.TransportType);
      }
   }

   public class IndividualOtherProteinXmlSerializer : IndividualProteinXmlSerializer<IndividualOtherProtein>
   {
   }

   public abstract class ExpressionContainerXmlSerializerBase<TProteinExpressionContainer> : PKSimContainerXmlSerializer<TProteinExpressionContainer> where TProteinExpressionContainer : MoleculeExpressionContainer
   {
      public override void PerformMapping()
      {
         base.PerformMapping();
         Map(x => x.OrganPath);
         Map(x => x.GroupName);
         Map(x => x.ContainerName);
      }
   }

   public class ProteinExpressionContainerXmlSerializer : ExpressionContainerXmlSerializerBase<MoleculeExpressionContainer>
   {
   }

   public class TransporterExpressionContainerXmlSerializer : ExpressionContainerXmlSerializerBase<TransporterExpressionContainer>
   {
      public override void PerformMapping()
      {
         base.PerformMapping();
         Map(x => x.MembraneLocation);
         Map(x => x.CompartmentName);
         MapEnumerable(x => x.ProcessNames, x => x.AddProcessName);
      }
   }
}
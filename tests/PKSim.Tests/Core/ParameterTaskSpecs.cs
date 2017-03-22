using System.Collections.Generic;
using OSPSuite.BDDHelper;
using OSPSuite.BDDHelper.Extensions;
using OSPSuite.Core.Commands.Core;
using OSPSuite.Utility.Collections;
using FakeItEasy;
using PKSim.Core.Commands;
using PKSim.Core.Events;
using PKSim.Core.Model;
using PKSim.Core.Repositories;
using PKSim.Core.Services;
using OSPSuite.Core.Domain;
using OSPSuite.Core.Domain.Repositories;
using OSPSuite.Core.Domain.Services;
using OSPSuite.Core.Domain.UnitSystem;
using OSPSuite.Core.Events;
using OSPSuite.Core.Services;

namespace PKSim.Core
{
   public abstract class concern_for_ParameterTask : ContextSpecification<IParameterTask>
   {
      protected IProject _project;
      protected IDimension _volumeDimension;
      protected IParameter _parameter;
      protected IExecutionContext _executionContext;
      protected IFavoriteTask _favoriteTask;

      protected override void Context()
      {
         _executionContext = A.Fake<IExecutionContext>();
         _favoriteTask = A.Fake<IFavoriteTask>();
         _project = A.Fake<IProject>();
         sut = new ParameterTask(new EntityPathResolver(new ObjectPathFactoryForSpecs()), _executionContext,_favoriteTask);
         var dimensionFactory = new DimensionFactory();
         _volumeDimension = dimensionFactory.AddDimension(new BaseDimensionRepresentation {LengthExponent = 3}, "Volume", "L");
         _volumeDimension.AddUnit("mL", 1e-3, 0);
         _parameter = DomainHelperForSpecs.ConstantParameterWithValue(10);
         _parameter.Dimension = _volumeDimension;
         A.CallTo(() => _executionContext.BuildingBlockContaining(_parameter)).Returns(A.Fake<IPKSimBuildingBlock>());
      }
   }

   public class When_asked_to_set_a_value_for_a_parameter : concern_for_ParameterTask
   {
      private ICommand _result;
      private double _valueToSetInGuiUnit;

      protected override void Context()
      {
         base.Context();
         _parameter.DisplayUnit = _volumeDimension.Unit("mL");
         _valueToSetInGuiUnit = 1000; //1000 ml
      }

      protected override void Because()
      {
         _result = sut.SetParameterDisplayValue(_parameter, _valueToSetInGuiUnit);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_parameter_value()
      {
         _result.ShouldBeAnInstanceOf<SetParameterValueCommand>();
      }

      [Observation]
      public void the_value_of_the_parameter_should_have_been_set()
      {
         _parameter.Value.ShouldBeEqualTo(1); //1L           
      }
   }

   public class When_asked_to_set_a_value_for_a_parameter_that_requires_a_structure_change : concern_for_ParameterTask
   {
      private ICommand _result;
      private double _valueToSetInGuiUnit;

      protected override void Context()
      {
         base.Context();
         _parameter.Name = Constants.Parameters.PARTICLE_SIZE_DISTRIBUTION;
         _parameter.DisplayUnit = _volumeDimension.Unit("mL");
         _valueToSetInGuiUnit = 1000; //1000 ml
      }

      protected override void Because()
      {
         _result = sut.SetParameterDisplayValue(_parameter, _valueToSetInGuiUnit);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_parameter_value()
      {
         _result.ShouldBeAnInstanceOf<SetParameterValueStructureChangeCommand>();
      }

      [Observation]
      public void the_value_of_the_parameter_should_have_been_set()
      {
         _parameter.Value.ShouldBeEqualTo(1); //1L           
      }
   }

   public class When_asked_to_set_a_value_for_an_advanced_paraemter : concern_for_ParameterTask
   {
      private int _valueToSetInGuiUnit;
      private ICommand _result;
      private IAdvancedParameterInPopulationUpdater _advancedParameterPopulationUpdater;

      protected override void Context()
      {
         base.Context();
         _advancedParameterPopulationUpdater = A.Fake<IAdvancedParameterInPopulationUpdater>();
         _parameter.DisplayUnit = _volumeDimension.Unit("mL");
         _valueToSetInGuiUnit = 1000; //1000 ml
         A.CallTo(() => _executionContext.Resolve<IAdvancedParameterInPopulationUpdater>()).Returns(_advancedParameterPopulationUpdater);
      }

      protected override void Because()
      {
         _result = sut.SetAdvancedParameterDisplayValue(_parameter, _valueToSetInGuiUnit);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_advanced_parameter_value()
      {
         _result.ShouldBeAnInstanceOf<SetAdvancedParameterValueCommand>();
      }

      [Observation]
      public void should_set_the_value_of_the_advanced_parameter_to_the_according_value()
      {
         _parameter.Value.ShouldBeEqualTo(1); //1L           
      }
   }

   public class When_setting_a_value_description_for_a_parameter_with_origin : concern_for_ParameterTask
   {
      private IParameter _originParameter;

      protected override void Context()
      {
         base.Context();
         _originParameter = A.Fake<IParameter>();
         _parameter.Origin.ParameterId = "Origin";
         A.CallTo(() => _executionContext.Get<IParameter>(_parameter.Origin.ParameterId)).Returns(_originParameter);
      }
      protected override void Because()
      {
         sut.SetParameterValueDescription(_parameter, "TEXT");
      }

      [Observation]
      public void should_update_the_value_description_of_this_parameter()
      {
         _parameter.ValueDescription.ShouldBeEqualTo("TEXT");
      }

      [Observation]
      public void should_update_the_value_description_of_the_origin_parameter()
      {
         _originParameter.ValueDescription.ShouldBeEqualTo("TEXT");
      }
   }

   public class When_setting_a_value_description_for_a_parameter_without_origin : concern_for_ParameterTask
   {
      protected override void Context()
      {
         base.Context();
         A.CallTo(() => _executionContext.Get<IParameter>(A<string>._)).Returns(null);
      }

      protected override void Because()
      {
         sut.SetParameterValueDescription(_parameter, "TEXT");
      }

      [Observation]
      public void should_update_the_value_description_of_this_parameter()
      {
         _parameter.ValueDescription.ShouldBeEqualTo("TEXT");
      }

   }

   public class When_asked_to_set_a_percentile_for_a_distributed_parameter : concern_for_ParameterTask
   {
      private ICommand _result;
      private IDistributedParameter _distributedParameter;
      private double _percentileToSet;

      protected override void Context()
      {
         base.Context();
         _percentileToSet = 0.8;
         _distributedParameter = DomainHelperForSpecs.NormalDistributedParameter();
         A.CallTo(() => _executionContext.BuildingBlockContaining(_distributedParameter)).Returns(A.Fake<IPKSimBuildingBlock>());
      }

      protected override void Because()
      {
         _result = sut.SetParameterPercentile(_distributedParameter, _percentileToSet);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_percentile_value()
      {
         _result.ShouldBeAnInstanceOf<SetParameterPercentileCommand>();
      }

      [Observation]
      public void the_percentile_of_the_parameter_should_have_been_set()
      {
         _distributedParameter.Percentile.ShouldBeEqualTo(_percentileToSet);
      }
   }

   public class When_asked_to_set_a_percentile_for_a_non_distributed_parameter : concern_for_ParameterTask
   {
      private ICommand _result;
      private double _percentileToSet;

      protected override void Context()
      {
         base.Context();
         _percentileToSet = 2.5;
      }

      protected override void Because()
      {
         _result = sut.SetParameterPercentile(_parameter, _percentileToSet);
      }

      [Observation]
      public void should_return_an_empty_command()
      {
         _result.ShouldBeAnInstanceOf<PKSimEmptyCommand>();
      }
   }

   public class When_setting_the_unit_of_a_parameter : concern_for_ParameterTask
   {
      private ICommand _result;
      private Unit _unitToSet;

      protected override void Context()
      {
         base.Context();
         _parameter.Value = 10; //in L
         _parameter.DisplayUnit = _volumeDimension.DefaultUnit; //L
         _unitToSet = _volumeDimension.Unit("mL");
      }

      protected override void Because()
      {
         _result = sut.SetParameterUnit(_parameter, _unitToSet);
      }

      [Observation]
      public void the_unit_of_the_parameter_should_have_been_set_to_the_given_unit()
      {
         _parameter.DisplayUnit.ShouldBeEqualTo(_unitToSet);
      }

      [Observation]
      public void the_value_of_parameter_should_have_been_set_updated()
      {
         _parameter.Value.ShouldBeEqualTo(0.01);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_parameter_value()
      {
         _result.ShouldBeAnInstanceOf<SetParameterUnitCommand>();
      }
   }

   public class When_setting_the_unit_of_a_parameter_requiring_a_structural_change_command : concern_for_ParameterTask
   {
      private ICommand _result;
      private Unit _unitToSet;

      protected override void Context()
      {
         base.Context();
         _parameter.Name = CoreConstants.Parameter.PARTICLE_RADIUS_MAX;
         _parameter.Value = 10; //in L
         _parameter.DisplayUnit = _volumeDimension.DefaultUnit; //L
         _unitToSet = _volumeDimension.Unit("mL");
      }

      protected override void Because()
      {
         _result = sut.SetParameterUnit(_parameter, _unitToSet);
      }

      [Observation]
      public void the_unit_of_the_parameter_should_have_been_set_to_the_given_unit()
      {
         _parameter.DisplayUnit.ShouldBeEqualTo(_unitToSet);
      }

      [Observation]
      public void the_value_of_parameter_should_have_been_set_updated()
      {
         _parameter.Value.ShouldBeEqualTo(0.01);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_parameter_value()
      {
         _result.ShouldBeAnInstanceOf<SetParameterUnitStructureChangeCommand>();
      }
   }


   public class When_asked_to_rename_a_parameter : concern_for_ParameterTask
   {
      private ICommand _result;
      private string _oldName;
      private string _newName;

      protected override void Context()
      {
         base.Context();
         _newName = "_newName";
         _oldName = "_oldName";
         _parameter.Name = _oldName;
      }

      protected override void Because()
      {
         _result = sut.SetParameterName(_parameter, _newName);
      }

      [Observation]
      public void the_name_of_the_parameter_should_have_been_set_to_the_new_name()
      {
         _parameter.Name.ShouldBeEqualTo(_newName);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_set_the_parameter_name()
      {
         _result.ShouldBeAnInstanceOf<RenameEntityCommand>();
      }
   }

   public class When_asked_to_reset_a_set_of_parameters_to_their_original_values : concern_for_ParameterTask
   {
      private ICommand _result;
      private IEnumerable<IParameter> _parameters;

      protected override void Context()
      {
         base.Context();
         var para1 = DomainHelperForSpecs.ConstantParameterWithValue(1).WithName("para1");
         var para2 = DomainHelperForSpecs.ConstantParameterWithValue(2).WithName("para1");
         _parameters = new List<IParameter> {para1, para2};
         A.CallTo(() => _executionContext.BuildingBlockContaining(para1)).Returns(A.Fake<IPKSimBuildingBlock>());
         A.CallTo(() => _executionContext.BuildingBlockContaining(para2)).Returns(A.Fake<IPKSimBuildingBlock>());
      }

      protected override void Because()
      {
         _result = sut.ResetParameters(_parameters);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_reset_the_parameters()
      {
         _result.ShouldBeAnInstanceOf<ResetParametersCommand>();
      }
   }

   public class When_asked_to_scale_a_set_of_parameters_with_a_given_factor : concern_for_ParameterTask
   {
      private ICommand _result;
      private IEnumerable<IParameter> _parameters;

      protected override void Context()
      {
         base.Context();
         var para1 = DomainHelperForSpecs.ConstantParameterWithValue(1).WithName("para1").WithDimension(A.Fake<IDimension>());
         var para2 = DomainHelperForSpecs.ConstantParameterWithValue(2).WithName("para1").WithDimension(A.Fake<IDimension>());
         A.CallTo(() => _executionContext.BuildingBlockContaining(para1)).Returns(A.Fake<IPKSimBuildingBlock>());
         A.CallTo(() => _executionContext.BuildingBlockContaining(para2)).Returns(A.Fake<IPKSimBuildingBlock>());
         _parameters = new List<IParameter> {para1, para2};
      }

      protected override void Because()
      {
         _result = sut.ScaleParameters(_parameters, 2);
      }

      [Observation]
      public void should_return_the_underlying_command_used_to_scale_the_parameters()
      {
         _result.ShouldBeAnInstanceOf<ScaleParametersCommand>();
      }
   }

   public class When_grouping_the_parameter_expressions : concern_for_ParameterTask
   {
      private IParameter _relExpPlasma;
      private IParameter _relExpPlasmaNorm;
      private IParameter _relExpLiver;
      private IParameter _relExpLiverNorm;
      private IParameter _relExpKidney;
      private IParameter _relExpKidneyNorm;
      private ICache<IParameter, IParameter> _result;
      private IParameter _anotherParameter;
      private IParameter _relExpWithoutNorm;

      protected override void Context()
      {
         base.Context();
         var organism = new Container().WithName("Organism");
         var kidney = new Container().WithName("Kidney").WithParentContainer(organism);
         var liver = new Container().WithName("Liver").WithParentContainer(organism);
         var bone = new Container().WithName("Bone").WithParentContainer(organism);
         _relExpPlasma = new PKSimParameter().WithName(CoreConstants.Parameter.RelExpPlasma).WithParentContainer(organism);
         _relExpPlasmaNorm = new PKSimParameter().WithName(CoreConstants.Parameter.RelExpPlasmaNorm).WithParentContainer(organism);
         _relExpLiver = new PKSimParameter().WithName(CoreConstants.Parameter.RelExp).WithParentContainer(liver);
         _relExpLiverNorm = new PKSimParameter().WithName(CoreConstants.Parameter.RelExpNorm).WithParentContainer(liver);
         _relExpKidney = new PKSimParameter().WithName(CoreConstants.Parameter.RelExp).WithParentContainer(kidney);
         _relExpKidneyNorm = new PKSimParameter().WithName(CoreConstants.Parameter.RelExpNorm).WithParentContainer(kidney);
         _anotherParameter = new PKSimParameter().WithName("not_a_rel_exp").WithParentContainer(kidney);
         _relExpWithoutNorm = new PKSimParameter().WithName(CoreConstants.Parameter.RelExp).WithParentContainer(bone);
      }

      protected override void Because()
      {
         _result = sut.GroupExpressionParameters(new[] {_relExpKidney, _relExpLiverNorm, _relExpLiver, _relExpPlasma, _relExpWithoutNorm, _relExpPlasmaNorm, _relExpKidneyNorm, _anotherParameter});
      }

      [Observation]
      public void should_return_a_cache_contain_a_parameter_as_key_and_the_corresponding_norm_parameter_as_value_for_global_rel_exp_parameters()
      {
         _result[_relExpPlasma].ShouldBeEqualTo(_relExpPlasmaNorm);
      }

      [Observation]
      public void should_return_a_cache_contain_a_parameter_as_key_and_the_corresponding_norm_parameter_as_value_for_local_rel_exp_parameter()
      {
         _result[_relExpLiver].ShouldBeEqualTo(_relExpLiverNorm);
         _result[_relExpKidney].ShouldBeEqualTo(_relExpKidneyNorm);
      }

      [Observation]
      public void the_returned_cache_should_not_contain_any_entry_for_parameter_that_are_not_relative_expression_parameters()
      {
         _result.Contains(_anotherParameter).ShouldBeFalse();
      }

      [Observation]
      public void the_returned_cache_should_not_contain_any_entry_for_relative_expression_parameters_for_which_a_norm_was_not_found()
      {
         _result.Contains(_relExpWithoutNorm).ShouldBeFalse();
      }
   }

   public class When_a_paraemter_is_set_as_favorite : concern_for_ParameterTask
   {
      private string _parameterPath;
      private IEntityPathResolver _entityPathResolver;
      private AddParameterToFavoritesEvent _event;

      protected override void Context()
      {
         base.Context();
         _entityPathResolver = A.Fake<IEntityPathResolver>();
         _parameter = new PKSimParameter();
         _parameterPath = "TOTO";
         sut = new ParameterTask(_entityPathResolver, _executionContext,_favoriteTask);
         A.CallTo(() => _entityPathResolver.PathFor(_parameter)).Returns(_parameterPath);
         A.CallTo(() => _executionContext.PublishEvent(A<AddParameterToFavoritesEvent>._))
            .Invokes(x => _event = x.GetArgument<AddParameterToFavoritesEvent>(0));
      }

      protected override void Because()
      {
         sut.SetParameterFavorite(_parameter, true);
      }

      [Observation]
      public void should_throw_the_event_specifing_that_the_parameter_was_set_as_favorite()
      {
         A.CallTo(() => _favoriteTask.SetParameterFavorite(_parameter, true)).MustHaveHappened();
      }
   }

   public class When_a_paraemter_is_removed_from_the_favorites : concern_for_ParameterTask
   {
      private string _parameterPath;
      private IEntityPathResolver _entityPathResolver;
      private RemoveParameterFromFavoritesEvent _event;

      protected override void Context()
      {
         base.Context();
         _entityPathResolver = A.Fake<IEntityPathResolver>();
         _parameter = new PKSimParameter();
         _parameterPath = "TRALALA";
         sut = new ParameterTask(_entityPathResolver, _executionContext,_favoriteTask);
         A.CallTo(() => _entityPathResolver.PathFor(_parameter)).Returns(_parameterPath);

         A.CallTo(() => _executionContext.PublishEvent(A<RemoveParameterFromFavoritesEvent>._))
            .Invokes(x => _event = x.GetArgument<RemoveParameterFromFavoritesEvent>(0));
      }

      protected override void Because()
      {
         sut.SetParameterFavorite(_parameter, false);
      }

      [Observation]
      public void should_throw_the_event_specifing_that_the_parameter_was_remove_from_the_favorites()
      {
         A.CallTo(() => _favoriteTask.SetParameterFavorite(_parameter, false)).MustHaveHappened();
         
      }
   }
}
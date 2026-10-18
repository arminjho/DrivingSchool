import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';
import 'city_tab.dart';
import 'country_tab.dart';
import 'driving_zone_tab.dart';
import 'question_area_tab.dart';
import 'request_status_tab.dart';
import 'service_type_tab.dart';
import 'vehicle_category_tab.dart';

/// 4.1.5 - one screen, seven tabs: the shared "šifarnik" (reference-data) CRUD shape repeated
/// once per entity, so admins manage every static lookup table from one place.
class ReferenceDataScreen extends StatefulWidget {
  const ReferenceDataScreen({super.key});

  @override
  State<ReferenceDataScreen> createState() => _ReferenceDataScreenState();
}

class _ReferenceDataScreenState extends State<ReferenceDataScreen> with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 7, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final c = context.colors;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Container(
          decoration: BoxDecoration(border: Border(bottom: BorderSide(color: c.border))),
          child: TabBar(
            controller: _tabController,
            isScrollable: true,
            tabAlignment: TabAlignment.start,
            tabs: const [
              Tab(icon: Icon(Icons.flag_outlined, size: 18), text: AppStrings.tabCountries),
              Tab(icon: Icon(Icons.location_city_outlined, size: 18), text: AppStrings.tabCities),
              Tab(icon: Icon(AppIcons.vehicle, size: 18), text: AppStrings.tabVehicleCategories),
              Tab(icon: Icon(Icons.rule_outlined, size: 18), text: AppStrings.tabRequestStatuses),
              Tab(icon: Icon(AppIcons.price, size: 18), text: AppStrings.tabServiceTypes),
              Tab(icon: Icon(AppIcons.place, size: 18), text: AppStrings.tabDrivingZones),
              Tab(icon: Icon(Icons.quiz_outlined, size: 18), text: AppStrings.tabQuestionAreas),
            ],
          ),
        ),
        Expanded(
          child: Padding(
            padding: AppSpacing.page,
            child: TabBarView(
              controller: _tabController,
              children: const [
                CountryTab(),
                CityTab(),
                VehicleCategoryTab(),
                RequestStatusTab(),
                ServiceTypeTab(),
                DrivingZoneTab(),
                QuestionAreaTab(),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

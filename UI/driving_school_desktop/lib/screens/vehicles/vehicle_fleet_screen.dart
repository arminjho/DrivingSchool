import 'package:material_ui/material_ui.dart';

import '../../core/design_tokens.dart';
import '../../l10n/app_strings.dart';
import 'vehicle_services_tab.dart';
import 'vehicles_tab.dart';

/// 4.1.3 - "Vozni park": two sub-tabs, Vozila and Servisi.
class VehicleFleetScreen extends StatefulWidget {
  const VehicleFleetScreen({super.key});

  @override
  State<VehicleFleetScreen> createState() => _VehicleFleetScreenState();
}

class _VehicleFleetScreenState extends State<VehicleFleetScreen> with SingleTickerProviderStateMixin {
  late final TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
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
              Tab(text: AppStrings.vehiclesTab),
              Tab(text: AppStrings.vehicleServicesTab),
            ],
          ),
        ),
        Expanded(
          child: Padding(
            padding: AppSpacing.page,
            child: TabBarView(
              controller: _tabController,
              children: const [VehiclesTab(), VehicleServicesTab()],
            ),
          ),
        ),
      ],
    );
  }
}

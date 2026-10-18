import '../core/auth_session.dart';
import '../core/base_provider.dart';
import '../models/country.dart';

class CountryProvider extends BaseProvider<Country> {
  CountryProvider(AuthSession session) : super('countries', session);

  @override
  Country fromJson(Map<String, dynamic> json) => Country.fromJson(json);
}

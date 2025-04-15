package com.memlastic.config;

import com.memlastic.converter.ByteArrayConverter;
import com.memlastic.properties.AppConfig;
import lombok.Data;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.converter.HttpMessageConverter;
import org.springframework.web.servlet.config.annotation.WebMvcConfigurer;

import java.util.List;

/**
 * A configuration class that adapts the context of the spring application. Adds a custom version of the converter.
 */
@Data
@Configuration
public class WebConfig implements WebMvcConfigurer {
    @Autowired
    private AppConfig appConfig;

    @Override
    public void configureMessageConverters(List<HttpMessageConverter<?>> converters) {
        converters.add(0, new ByteArrayConverter(appConfig.getLimit()));
    }
}